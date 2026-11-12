using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Web;
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
                int? page,
                int? pageSize,
                IQueryHandler<GetProductsQuery, PagedResult<ProductDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(type ?? 0, PageRequest.From(page, pageSize)), ct)))
            .Produces<PagedResult<ProductDto>>();

        group.MapGet("/{id:int}", async (
                int id,
                IQueryHandler<GetProductDetailQuery, Result<ProductDetailDto>> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(id), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .Produces<ProductDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
