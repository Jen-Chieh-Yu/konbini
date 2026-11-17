using System.Linq.Expressions;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Orders.Repositories;

public interface IOrderRepository : IRepository
{
    /// <summary>某使用者的訂單（含明細），依建立時間新到舊。</summary>
    Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct);

    /// <summary>某使用者的一筆訂單（含明細）；不存在或不屬於這個使用者時回 null。</summary>
    Task<OrderDto?> GetByIdAsync(int id, int userId, CancellationToken ct);

    /// <summary>加入新訂單（含明細）；由呼叫端以 IUnitOfWork 提交。</summary>
    void Add(Order order);
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
}
