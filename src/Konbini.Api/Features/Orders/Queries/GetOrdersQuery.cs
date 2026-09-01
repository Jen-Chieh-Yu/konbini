using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Repositories;

namespace Konbini.Api.Features.Orders.Queries;

public record GetOrdersQuery(int UserId);

public class GetOrdersHandler(IOrderRepository orders)
    : IQueryHandler<GetOrdersQuery, List<OrderDto>>
{
    public async Task<List<OrderDto>> Handle(GetOrdersQuery query, CancellationToken ct)
        => await orders.GetByUserAsync(query.UserId, ct);
}
