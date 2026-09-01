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
    Task<List<ProductDto>> GetListAsync(int type, CancellationToken ct);

    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>同類別的其他商品（相關商品）。</summary>
    Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct);

    /// <summary>多關鍵字 OR 搜尋，任一命中即符合。</summary>
    Task<List<ProductDto>> SearchAsync(string[] keywords, CancellationToken ct);

    /// <summary>依 Id 取商品快照（下單計價用）；查無的 Id 不在結果內。</summary>
    Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}

public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<List<ProductDto>> GetListAsync(int type, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => type == 0 || p.Type == type)
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .ToListAsync(ct);

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .FirstOrDefaultAsync(ct);

    public async Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => p.Type == type && p.Id != excludeId)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .ToListAsync(ct);

    public async Task<List<ProductDto>> SearchAsync(string[] keywords, CancellationToken ct)
    {
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

    public async Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProductSnapshot(p.Id, p.Name, p.Price, p.ImageUrl))
            .ToDictionaryAsync(p => p.Id, ct);
}
