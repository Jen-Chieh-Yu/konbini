namespace Konbini.Api.Features.Orders.Models;

public class OrderItem
{
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
