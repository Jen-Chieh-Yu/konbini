using Konbini.Api.Features.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Common.Persistence;

public static class PagingExtensions
{
    /// <summary>呼叫前要先排序，且排序要含唯一鍵，否則換頁時資料可能重複或遺漏。</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PageRequest paging, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(paging.Offset).Take(paging.PageSize).ToListAsync(ct);
        return new(items, paging.Page, paging.PageSize, totalCount);
    }
}
