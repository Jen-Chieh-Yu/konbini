using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Orders.Models;

public class Order : AuditableEntity
{
    // 資料庫欄位長度的單一來源：OrderConfiguration 與 Handler 的輸入驗證共用
    public const int ContactNameMaxLength = 100;
    public const int ContactPhoneMaxLength = 20;
    public const int StreetAddressMaxLength = 200;
    public const int MemoMaxLength = 500;

    /// <summary>
    /// 一張訂單最多幾筆 items（同商品分多筆也各算一筆）。購物車實際不會超過幾十項，
    /// 上限讓錯誤訊息與查詢的 Id 數量有界。
    /// </summary>
    public const int MaxItems = 50;

    public int Id { get; set; }
    public int UserId { get; set; }
    public int Subtotal { get; set; }
    public int DeliveryFee { get; set; }
    public int Total { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public int DeliveryMethod { get; set; }
    public int CityCode { get; set; }
    public int DistrictCode { get; set; }
    public string StreetAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? Memo { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
