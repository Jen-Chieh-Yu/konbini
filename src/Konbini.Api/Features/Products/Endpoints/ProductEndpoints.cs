using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Queries;

namespace Konbini.Api.Features.Products.Endpoints;

public class ProductEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", async (
                int? type,
                IQueryHandler<GetProductsQuery, List<ProductDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(type ?? 0), ct)));

        group.MapGet("/{id:int}", async (
                int id,
                IQueryHandler<GetProductDetailQuery, ProductDetailDto?> handler,
                CancellationToken ct)
            => await handler.Handle(new(id), ct) is { } detail
                ? Results.Ok(detail)
                : Results.NotFound());
    }
}
