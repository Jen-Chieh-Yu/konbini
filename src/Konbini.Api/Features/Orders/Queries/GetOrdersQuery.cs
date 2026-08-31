using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Orders.Queries;

public record GetOrdersQuery(int UserId);

public class GetOrdersHandler(AppDbContext db)
    : IQueryHandler<GetOrdersQuery, List<OrderDto>>
{
    public async Task<List<OrderDto>> Handle(GetOrdersQuery query, CancellationToken ct)
        => await db.Orders.AsNoTracking()
            .Where(o => o.UserId == query.UserId)
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
}
