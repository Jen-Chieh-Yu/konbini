using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Errors;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Products.Queries;

public record GetProductDetailQuery(int Id);

public class GetProductDetailHandler(IProductRepository products)
    : IQueryHandler<GetProductDetailQuery, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(GetProductDetailQuery query, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(query.Id, ct);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        var relevant = await products.GetRelatedAsync(product.Type, product.Id, ct);
        return new ProductDetailDto(product, relevant);
    }
}
