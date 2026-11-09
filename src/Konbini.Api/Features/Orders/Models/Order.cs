using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Orders.Models;

public class Order : AuditableEntity
{
    // 資料庫欄位長度的單一來源：OrderConfiguration 與 Handler 的輸入驗證共用
    public const int ContactNameMaxLength = 100;
    public const int ContactPhoneMaxLength = 20;
    public const int StreetAddressMaxLength = 200;
    public const int MemoMaxLength = 500;

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
