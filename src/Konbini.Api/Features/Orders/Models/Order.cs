namespace Konbini.Api.Features.Orders.Models;

public class Order
{
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = [];
}
