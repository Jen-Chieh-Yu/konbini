using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Products.Models;

public class Product : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>商品類別：0 保留給「全部」查詢；1 零食、2 泡麵、3 飲品。</summary>
    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 庫存件數，0 表示售完；不會小於 0（資料庫的 ck_products_stock 也會擋）。
    /// 下單時用條件式 UPDATE 扣（ProductRepository.TryDeductStockAsync），不要讀出來在 C# 裡減完再存回去：
    /// 兩筆訂單同時讀到同一個數字就會超賣。
    /// </summary>
    public int Stock { get; set; }
}
