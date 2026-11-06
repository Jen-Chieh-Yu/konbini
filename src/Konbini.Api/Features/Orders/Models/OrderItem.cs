namespace Konbini.Api.Features.Orders.Models;

public class OrderItem
{
    /// <summary>每項商品的數量上限。沒有上限時，單價 × 數量可能溢位，算出錯誤的訂單金額。</summary>
    public const int MaxQuantity = 99;

    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }

    /// <summary>下單當下的商品名與單價快照，之後商品改價不影響歷史訂單。</summary>
    public string ProductName { get; set; } = string.Empty;
    public int UnitPrice { get; set; }

    public int Quantity { get; set; }
    public int Subtotal { get; set; }
    public string? ImageUrl { get; set; }
}
