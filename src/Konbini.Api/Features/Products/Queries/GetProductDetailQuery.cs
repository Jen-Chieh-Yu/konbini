using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Products.Queries;

public record GetProductDetailQuery(int Id);

public class GetProductDetailHandler(IProductRepository products)
    : IQueryHandler<GetProductDetailQuery, ProductDetailDto?>
{
    public async Task<ProductDetailDto?> Handle(GetProductDetailQuery query, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(query.Id, ct);
        if (product is null)
        {
            return null;
        }

        var relevant = await products.GetRelatedAsync(product.Type, product.Id, ct);
        return new ProductDetailDto(product, relevant);
    }
}
