using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Products.Queries;

/// <param name="Type">商品類別；0 = 全部</param>
public record GetProductsQuery(int Type);

public class GetProductsHandler(IProductRepository products)
    : IQueryHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
        => await products.GetListAsync(query.Type, ct);
}
