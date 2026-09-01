using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Search.Queries;

/// <param name="Keyword">空白分隔的多關鍵字，任一命中即符合。</param>
public record SearchProductsQuery(string Keyword);

public class SearchProductsHandler(IProductRepository products)
    : IQueryHandler<SearchProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(SearchProductsQuery query, CancellationToken ct)
    {
        // 關鍵字拆解屬輸入處理，留在 Handler；查詢組裝在 Repository
        var keywords = query.Keyword.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return await products.SearchAsync(keywords, ct);
    }
}
