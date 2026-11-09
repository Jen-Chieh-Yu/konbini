using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Products.Repositories;

/// <summary>下單計價用的商品快照投影，不把 entity 外洩到其他 feature。</summary>
public record ProductSnapshot(int Id, string Name, int Price, string? ImageUrl);

public interface IProductRepository : IRepository
{
    /// <param name="type">商品類別；0 = 全部</param>
    Task<PagedResult<ProductDto>> GetListAsync(int type, PageRequest paging, CancellationToken ct);

    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>同類別的其他商品（相關商品），最多 8 筆。</summary>
    Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct);

    /// <summary>多關鍵字 OR 搜尋，任一命中即符合。</summary>
    Task<PagedResult<ProductDto>> SearchAsync(string[] keywords, PageRequest paging, CancellationToken ct);

    /// <summary>依 Id 取商品快照（下單計價用）；查無的 Id 不在結果內。</summary>
    Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}

public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    private const int RelatedLimit = 8;

    public async Task<PagedResult<ProductDto>> GetListAsync(int type, PageRequest paging, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => type == 0 || p.Type == type)
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .ToPagedResultAsync(paging, ct);

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .FirstOrDefaultAsync(ct);

    public async Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => p.Type == type && p.Id != excludeId)
            // PostgreSQL 不保證未排序查詢的順序（MySQL InnoDB 依主鍵叢集，過去「剛好」有序）
            .OrderBy(p => p.Id)
            .Take(RelatedLimit)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .ToListAsync(ct);

    public async Task<PagedResult<ProductDto>> SearchAsync(string[] keywords, PageRequest paging, CancellationToken ct)
    {
        if (keywords.Length == 0)
        {
            return new([], paging.Page, paging.PageSize, 0);
        }

        // 多關鍵字 OR：以 Union 累加，可完整轉譯為 SQL（不需在記憶體過濾）
        // ILike 不分大小寫（PostgreSQL 的 LIKE／Contains 會區分）；% 與 _ 先跳脫，避免使用者輸入被當成萬用字元
        IQueryable<Product>? matched = null;
        foreach (var keyword in keywords)
        {
            var pattern = $"%{EscapeLike(keyword)}%";
            var part = db.Products.Where(p => EF.Functions.ILike(p.Name, pattern, LikeEscape));
            matched = matched is null ? part : matched.Union(part);
        }

        return await matched!.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .ToPagedResultAsync(paging, ct);
    }

    private const string LikeEscape = "\\";

    private static string EscapeLike(string value)
        => value.Replace(LikeEscape, LikeEscape + LikeEscape)
            .Replace("%", LikeEscape + "%")
            .Replace("_", LikeEscape + "_");

    public async Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProductSnapshot(p.Id, p.Name, p.Price, p.ImageUrl))
            .ToDictionaryAsync(p => p.Id, ct);
}
