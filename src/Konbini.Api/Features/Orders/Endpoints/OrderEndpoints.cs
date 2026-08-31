using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
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
                ICurrentUser currentUser,
                IQueryHandler<GetOrdersQuery, List<OrderDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(currentUser.Id), ct)));

        group.MapPost("/", async (
                CreateOrderRequest request,
                ICurrentUser currentUser,
                ICommandHandler<CreateOrderCommand, CreateOrderResult> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(currentUser.Id, request), ct);
                return result.Success
                    ? Results.Created($"/api/orders/{result.OrderId}", result)
                    : Results.BadRequest(result);
            });
    }
}
