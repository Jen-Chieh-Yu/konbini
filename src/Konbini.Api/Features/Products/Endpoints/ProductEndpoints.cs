using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Web;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Queries;

namespace Konbini.Api.Features.Products.Endpoints;

public class ProductEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        // ids 用重複的參數傳（?ids=1&ids=2），Minimal API 直接綁成 int[]；不是整數時回 400
        group.MapGet("/", async (
                int? type,
                int[]? ids,
                int? page,
                int? pageSize,
                IQueryHandler<GetProductsQuery, PagedResult<ProductDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(
                new(type ?? 0, ids ?? [], PageRequest.From(page, pageSize)), ct)))
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

        // --- 後台：/api/admin/products，Admin policy 掛在 group 上 ---
        var admin = app.MapAdminGroup("products");

        admin.MapGet("/", async (
                int? page,
                int? pageSize,
                IQueryHandler<GetAdminProductsQuery, PagedResult<AdminProductDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(PageRequest.From(page, pageSize)), ct)))
            .Produces<PagedResult<AdminProductDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
