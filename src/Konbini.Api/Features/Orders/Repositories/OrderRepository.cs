using System.Linq.Expressions;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Orders.Repositories;

/// <summary>取消訂單前要知道的狀態與明細件數（歸還庫存用）。</summary>
public record OrderCancelSnapshot(OrderStatus Status, List<OrderStockLine> Items);

public record OrderStockLine(int ProductId, int Quantity);

public interface IOrderRepository : IRepository
{
    /// <summary>某使用者的訂單（含明細），依建立時間新到舊。</summary>
    Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct);

    /// <summary>某使用者的一筆訂單（含明細）；不存在或不屬於這個使用者時回 null。</summary>
    Task<OrderDto?> GetByIdAsync(int id, int userId, CancellationToken ct);

    /// <summary>加入新訂單（含明細）；由呼叫端以 IUnitOfWork 提交。</summary>
    void Add(Order order);

    /// <summary>某使用者的一筆訂單的狀態與明細件數；不存在或不屬於這個使用者時回 null。</summary>
    Task<OrderCancelSnapshot?> GetCancelSnapshotAsync(int id, int userId, CancellationToken ct);

    /// <summary>
    /// 訂單還是處理中時改成已取消並回 true；已經不是處理中（例：同時送出的另一次取消先完成）時不改資料，回 false。
    /// 立即執行（ExecuteUpdate），不等 SaveChanges：要和歸還庫存一起成立時，呼叫端先開 IUnitOfWork 的交易。
    /// </summary>
    /// <param name="userId">下單的使用者，同時是異動者；ExecuteUpdate 不經過 AuditInterceptor，要自己設。</param>
    Task<bool> TryCancelAsync(int id, int userId, CancellationToken ct);
}

public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    // 列表與明細回傳同一個形狀。明細依建立順序（Id）排列：不指定的話，PostgreSQL 不保證每次順序相同
    private static readonly Expression<Func<Order, OrderDto>> ToDto = o => new OrderDto(
        o.Id,
        o.Subtotal,
        o.DeliveryFee,
        o.Total,
        o.ContactName,
        o.ContactPhone,
        o.DeliveryAddress,
        o.Memo,
        o.CreatedOn,   // API 契約維持 createdAt
        o.OrderStatus,
        o.CancelledOn,
        o.Items.OrderBy(i => i.Id)
            .Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal, i.ImageUrl))
            .ToList());

    public async Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct)
        => await db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedOn)
            .ThenByDescending(o => o.Id)   // 建立時間相同時順序才固定，換頁不會重複或遺漏
            .Select(ToDto)
            .ToPagedResultAsync(paging, ct);

    // 使用者 Id 和列表一樣放進查詢條件：別人的訂單不會被讀出來
    public async Task<OrderDto?> GetByIdAsync(int id, int userId, CancellationToken ct)
        => await db.Orders.AsNoTracking()
            .Where(o => o.Id == id && o.UserId == userId)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct);

    public void Add(Order order) => db.Orders.Add(order);

    public async Task<OrderCancelSnapshot?> GetCancelSnapshotAsync(int id, int userId, CancellationToken ct)
        => await db.Orders.AsNoTracking()
            .Where(o => o.Id == id && o.UserId == userId)
            .Select(o => new OrderCancelSnapshot(
                o.OrderStatus,
                o.Items.Select(i => new OrderStockLine(i.ProductId, i.Quantity)).ToList()))
            .FirstOrDefaultAsync(ct);

    // 「只有處理中能取消」由資料庫在同一個陳述式裡判斷：兩次取消同時送來時，只有一次改得到
    public async Task<bool> TryCancelAsync(int id, int userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var affected = await db.Orders
            .Where(o => o.Id == id && o.UserId == userId && o.OrderStatus == OrderStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.OrderStatus, OrderStatus.Cancelled)
                .SetProperty(o => o.CancelledOn, now)
                .SetProperty(o => o.ModifiedOn, now)
                .SetProperty(o => o.ModifiedBy, userId), ct);
        return affected == 1;
    }
}
