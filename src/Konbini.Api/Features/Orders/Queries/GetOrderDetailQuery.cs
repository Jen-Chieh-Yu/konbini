using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Orders.Errors;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Repositories;

namespace Konbini.Api.Features.Orders.Queries;

/// <summary>UserId 取自 token，不接受前端傳來的值。</summary>
public record GetOrderDetailQuery(int UserId, int Id);

/// <summary>
/// 訂單明細。擁有權在這裡把關：目前使用者交給 Repository 當查詢條件，
/// 查不到與不是自己的都回 404、不回 403，不讓人分辨訂單是否存在（api-conventions「授權」）。
/// </summary>
public class GetOrderDetailHandler(IOrderRepository orders)
    : IQueryHandler<GetOrderDetailQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderDetailQuery query, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(query.Id, query.UserId, ct);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        return order;
    }
}
