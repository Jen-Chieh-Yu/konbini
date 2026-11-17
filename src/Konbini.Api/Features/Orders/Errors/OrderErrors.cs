using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Orders.Errors;

/// <summary>Orders 的業務錯誤。欄位格式的檢查訊息寫在 Handler 的規則旁邊，不收在這裡。</summary>
public static class OrderErrors
{
    /// <summary>不存在、不屬於目前使用者、已軟刪除都回這一個，不讓人分辨訂單是否存在（api-conventions「授權」）。</summary>
    public static readonly Error NotFound = Error.NotFound("找不到訂單");

    /// <summary>body 裡參照到不存在的商品屬於輸入錯誤，回 400；404 只用在網址指到的資源。</summary>
    public static Error ProductsNotFound(IEnumerable<int> productIds) =>
        Error.Validation("items", $"下列商品不存在：{string.Join(", ", productIds)}");
}
