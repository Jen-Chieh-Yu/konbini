using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Search.Queries;

/// <param name="Keyword">空白分隔的多關鍵字，任一命中即符合。</param>
public record SearchProductsQuery(string Keyword);

public class SearchProductsHandler(AppDbContext db)
    : IQueryHandler<SearchProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(SearchProductsQuery query, CancellationToken ct)
    {
        var keywords = query.Keyword.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (keywords.Length == 0)
        {
            return [];
        }

        // 多關鍵字 OR：以 Union 累加，可完整轉譯為 SQL（不需在記憶體過濾）
        IQueryable<Product>? matched = null;
        foreach (var keyword in keywords)
        {
            var part = db.Products.Where(p => p.Name.Contains(keyword));
            matched = matched is null ? part : matched.Union(part);
        }

        return await matched!.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .ToListAsync(ct);
    }
}
