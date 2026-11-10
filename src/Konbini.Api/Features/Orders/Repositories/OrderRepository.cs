using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Orders.Repositories;

public interface IOrderRepository : IRepository
{
    /// <summary>某使用者的訂單（含明細），依建立時間新到舊。</summary>
    Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct);

    /// <summary>加入新訂單（含明細）；由呼叫端以 IUnitOfWork 提交。</summary>
    void Add(Order order);
}

public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct)
        => await db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedOn)
            .ThenByDescending(o => o.Id)   // 建立時間相同時順序才固定，換頁不會重複或遺漏
            .Select(o => new OrderDto(
                o.Id,
                o.Subtotal,
                o.DeliveryFee,
                o.Total,
                o.ContactName,
                o.ContactPhone,
                o.DeliveryAddress,
                o.Memo,
                o.CreatedOn,   // API 契約維持 createdAt
                o.Items.Select(i => new OrderItemDto(
                    i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal, i.ImageUrl))
                    .ToList()))
            .ToPagedResultAsync(paging, ct);

    public void Add(Order order) => db.Orders.Add(order);
}
