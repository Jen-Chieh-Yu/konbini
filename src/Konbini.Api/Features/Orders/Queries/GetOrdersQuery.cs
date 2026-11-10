using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Repositories;

namespace Konbini.Api.Features.Orders.Queries;

public record GetOrdersQuery(int UserId, PageRequest Paging);

public class GetOrdersHandler(IOrderRepository orders)
    : IQueryHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery query, CancellationToken ct)
        => await orders.GetByUserAsync(query.UserId, query.Paging, ct);
}
