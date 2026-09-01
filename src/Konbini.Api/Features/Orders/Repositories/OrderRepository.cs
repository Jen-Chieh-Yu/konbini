using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Orders.Repositories;

public interface IOrderRepository : IRepository
{
    /// <summary>某使用者的全部訂單（含明細），依建立時間新到舊。</summary>
    Task<List<OrderDto>> GetByUserAsync(int userId, CancellationToken ct);

    /// <summary>加入新訂單（含明細）；由呼叫端以 IUnitOfWork 提交。</summary>
    void Add(Order order);
}

public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<List<OrderDto>> GetByUserAsync(int userId, CancellationToken ct)
        => await db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderDto(
                o.Id,
                o.Subtotal,
                o.DeliveryFee,
                o.Total,
                o.ContactName,
                o.ContactPhone,
                o.DeliveryAddress,
                o.Memo,
                o.CreatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Subtotal, i.ImageUrl))
                    .ToList()))
            .ToListAsync(ct);

    public void Add(Order order) => db.Orders.Add(order);
}
