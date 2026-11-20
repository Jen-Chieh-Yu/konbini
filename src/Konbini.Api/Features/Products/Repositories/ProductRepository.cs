using System.Linq.Expressions;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Products.Repositories;

/// <summary>下單計價與檢查庫存用的商品快照投影，不把 entity 外洩到其他 feature。</summary>
public record ProductSnapshot(int Id, string Name, int Price, string? ImageUrl, int Stock);

public interface IProductRepository : IRepository
{
    /// <param name="type">商品類別；0 = 全部</param>
    /// <param name="ids">只取這些 Id 的商品；空集合 = 不限。和 type 同時給時兩個條件都要符合。</param>
    Task<PagedResult<ProductDto>> GetListAsync(int type, IReadOnlyCollection<int> ids, PageRequest paging, CancellationToken ct);

    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>同類別的其他商品（相關商品），最多 8 筆。</summary>
    Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct);

    /// <summary>多關鍵字 OR 搜尋，任一命中即符合。</summary>
    Task<PagedResult<ProductDto>> SearchAsync(string[] keywords, PageRequest paging, CancellationToken ct);

    /// <summary>依 Id 取商品快照（下單計價、檢查庫存用）；查無的 Id 不在結果內。</summary>
    Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    /// <summary>
    /// 庫存足夠時扣掉並回 true；不夠、商品不存在或已軟刪除時不改資料，回 false。
    /// 立即執行（ExecuteUpdate），不等 SaveChanges：要和訂單一起成立時，呼叫端先開 IUnitOfWork 的交易。
    /// </summary>
    /// <param name="modifiedBy">異動者的使用者 Id；ExecuteUpdate 不經過 AuditInterceptor，要自己傳。</param>
    Task<bool> TryDeductStockAsync(int id, int quantity, int? modifiedBy, CancellationToken ct);

    /// <summary>
    /// 把件數加回庫存（取消訂單）。已軟刪除的商品也加回去：庫存是實際收回的貨，商品之後恢復上架時件數才正確。
    /// 立即執行（ExecuteUpdate）：要和訂單狀態一起成立時，呼叫端先開 IUnitOfWork 的交易。
    /// </summary>
    /// <param name="modifiedBy">異動者的使用者 Id；ExecuteUpdate 不經過 AuditInterceptor，要自己傳。</param>
    Task RestockAsync(int id, int quantity, int? modifiedBy, CancellationToken ct);
}

public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    private const int RelatedLimit = 8;

    private static readonly Expression<Func<Product, ProductDto>> ToDto =
        p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl, p.Stock);

    public async Task<PagedResult<ProductDto>> GetListAsync(
        int type, IReadOnlyCollection<int> ids, PageRequest paging, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().Where(p => type == 0 || p.Type == type);
        // 沒給 Id 時不加條件，SQL 裡才不會多出一段永遠成立的 ANY(@ids)；
        // 軟刪除的商品由 HasQueryFilter 排除，和查不到的 Id 一樣不在結果內
        if (ids.Count > 0)
        {
            query = query.Where(p => ids.Contains(p.Id));
        }

        return await query
            .OrderBy(p => p.Id)
            .Select(ToDto)
            .ToPagedResultAsync(paging, ct);
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct);

    public async Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => p.Type == type && p.Id != excludeId)
            // PostgreSQL 不保證未排序查詢的順序（MySQL InnoDB 依主鍵叢集，過去「剛好」有序）
            .OrderBy(p => p.Id)
            .Take(RelatedLimit)
            .Select(ToDto)
            .ToListAsync(ct);

    public async Task<PagedResult<ProductDto>> SearchAsync(string[] keywords, PageRequest paging, CancellationToken ct)
    {
        if (keywords.Length == 0)
        {
            return new([], paging.Page, paging.PageSize, 0);
        }

        // 多關鍵字 OR：所有樣式放進同一個陣列參數，轉成 EXISTS (… unnest(@patterns) … ILIKE …)。
        // 運算式樹深度與關鍵字數量無關；原本每個關鍵字 Union 一次，深度隨數量增加，1000 個就 stack overflow
        // ILike 不分大小寫（PostgreSQL 的 LIKE／Contains 會區分）；% 與 _ 先跳脫，避免使用者輸入被當成萬用字元
        var patterns = keywords.Select(k => $"%{EscapeLike(k)}%").ToArray();

        return await db.Products.AsNoTracking()
            .Where(p => patterns.Any(pattern => EF.Functions.ILike(p.Name, pattern, LikeEscape)))
            .OrderBy(p => p.Id)
            .Select(ToDto)
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
            .Select(p => new ProductSnapshot(p.Id, p.Name, p.Price, p.ImageUrl, p.Stock))
            .ToDictionaryAsync(p => p.Id, ct);

    // 條件式 UPDATE：「夠不夠」和「扣掉」在同一個陳述式裡由資料庫判斷（UPDATE … WHERE stock >= @quantity），
    // 兩筆訂單同時扣同一項商品也不會超賣。HasQueryFilter 對 ExecuteUpdate 一樣生效，軟刪除的商品扣不到
    public async Task<bool> TryDeductStockAsync(int id, int quantity, int? modifiedBy, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var affected = await db.Products
            .Where(p => p.Id == id && p.Stock >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Stock, p => p.Stock - quantity)
                .SetProperty(p => p.ModifiedOn, now)
                .SetProperty(p => p.ModifiedBy, modifiedBy), ct);
        return affected == 1;
    }

    public async Task RestockAsync(int id, int quantity, int? modifiedBy, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.Products.IgnoreQueryFilters()
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Stock, p => p.Stock + quantity)
                .SetProperty(p => p.ModifiedOn, now)
                .SetProperty(p => p.ModifiedBy, modifiedBy), ct);
    }
}
