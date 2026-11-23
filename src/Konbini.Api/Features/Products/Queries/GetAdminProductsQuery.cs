using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Products.Queries;

public record GetAdminProductsQuery(PageRequest Paging);

/// <summary>後台的商品與庫存一覽。只有管理員能呼叫（Admin policy 掛在 route group），Handler 不再檢查角色。</summary>
public class GetAdminProductsHandler(IProductRepository products)
    : IQueryHandler<GetAdminProductsQuery, PagedResult<AdminProductDto>>
{
    public async Task<PagedResult<AdminProductDto>> Handle(GetAdminProductsQuery query, CancellationToken ct)
        => await products.GetAdminListAsync(query.Paging, ct);
}
