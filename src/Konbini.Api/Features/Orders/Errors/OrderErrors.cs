using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Orders.Errors;

/// <summary>Orders 的業務錯誤。欄位格式的檢查訊息寫在 Handler 的規則旁邊，不收在這裡。</summary>
public static class OrderErrors
{
    /// <summary>不存在、不屬於目前使用者、已軟刪除都回這一個，不讓人分辨訂單是否存在（api-conventions「授權」）。</summary>
    public static readonly Error NotFound = Error.NotFound("找不到訂單");

    /// <summary>body 裡參照到不存在的商品屬於輸入錯誤，回 400；404 只用在網址指到的資源。</summary>
    public static Error ProductsNotFound(IEnumerable<int> productIds) =>
        Error.Validation("items", $"下列商品不存在：{string.Join(", ", productIds)}");

    /// <summary>
    /// 庫存不足是和資料狀態的衝突，回 409。列品名、不列 Id，使用者才知道要改哪一項；
    /// 剩餘件數是檢查當下的值，之後可能又被買走。
    /// </summary>
    public static Error InsufficientStock(IEnumerable<ProductSnapshot> products) =>
        Error.Conflict("items", "下列商品庫存不足：" + string.Join("、", products.Select(p =>
            p.Stock > 0 ? $"{p.Name}（剩 {p.Stock} 件）" : $"{p.Name}（已售完）")));

    /// <summary>
    /// 訂單已經不是處理中，和資料狀態衝突，回 409。目前只有「已取消」會走到這裡；
    /// 之後有「已出貨」時，依狀態回不同的訊息。
    /// </summary>
    public static readonly Error AlreadyCancelled = Error.Conflict(Error.GeneralKey, "這筆訂單已經取消");
}
