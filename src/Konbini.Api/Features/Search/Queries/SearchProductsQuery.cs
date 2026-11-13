using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Search.Queries;

/// <param name="Keyword">空白（含全形空白）分隔的多關鍵字，任一命中即符合。</param>
public record SearchProductsQuery(string Keyword, PageRequest Paging);

public class SearchProductsHandler(IProductRepository products)
    : IQueryHandler<SearchProductsQuery, PagedResult<ProductDto>>
{
    /// <summary>關鍵字數量上限（去重後）；超過的不參與搜尋。前端 SearchView 的提示文字要對齊。</summary>
    public const int MaxKeywords = 10;

    /// <summary>單一關鍵字的長度上限（UTF-16 字元數）；超過的截斷。</summary>
    public const int MaxKeywordLength = 50;

    public async Task<PagedResult<ProductDto>> Handle(SearchProductsQuery query, CancellationToken ct)
        => await products.SearchAsync(ParseKeywords(query.Keyword), query.Paging, ct);

    /// <summary>
    /// 拆字屬輸入處理，留在 Handler；查詢組裝在 Repository。
    /// 和 pageSize 一樣超過上限就夾住、不回 400：搜尋是寬鬆的輸入，多打的字不值得擋下整次搜尋。
    /// </summary>
    private static string[] ParseKeywords(string keyword)
        => keyword
            // separator 傳 null 時以 char.IsWhiteSpace 判斷，涵蓋全形空白（U+3000）、Tab、換行
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(Truncate)
            // 搜尋用 ILike 不分大小寫，去重也不分；先去重再取上限，重複的字不佔名額
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxKeywords)
            .ToArray();

    private static string Truncate(string keyword)
    {
        if (keyword.Length <= MaxKeywordLength) return keyword;
        // 不切開代理對（emoji 等）：落單的半個字元不是合法的 UTF-8
        var length = char.IsHighSurrogate(keyword[MaxKeywordLength - 1]) ? MaxKeywordLength - 1 : MaxKeywordLength;
        return keyword[..length];
    }
}
