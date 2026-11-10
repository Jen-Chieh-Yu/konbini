using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Products.Queries;

/// <param name="Type">商品類別；0 = 全部</param>
public record GetProductsQuery(int Type, PageRequest Paging);

public class GetProductsHandler(IProductRepository products)
    : IQueryHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
        => await products.GetListAsync(query.Type, query.Paging, ct);
}
