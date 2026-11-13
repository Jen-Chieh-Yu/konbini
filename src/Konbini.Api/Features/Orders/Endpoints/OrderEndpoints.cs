using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Web;
using Konbini.Api.Features.Orders.Commands;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Queries;

namespace Konbini.Api.Features.Orders.Endpoints;

public class OrderEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapGet("/", async (
                int? page,
                int? pageSize,
                ICurrentUser currentUser,
                IQueryHandler<GetOrdersQuery, PagedResult<OrderDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(currentUser.Id, PageRequest.From(page, pageSize)), ct)))
            .Produces<PagedResult<OrderDto>>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
                CreateOrderRequest request,
                ICurrentUser currentUser,
                ICommandHandler<CreateOrderCommand, Result<CreateOrderResponse>> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(currentUser.Id, request), ct);
                // Location 指向的訂單明細路由尚未實作（CLAUDE.md §6）
                return result.IsSuccess
                    ? Results.Created($"/api/orders/{result.Value.Id}", result.Value)
                    : result.Error.ToProblem();
            })
            .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
