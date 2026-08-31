namespace Konbini.Api.Features.Products.Models;

public class Product
{
    public int Id { get; set; }

    /// <summary>商品類別：0 保留給「全部」查詢；1 零食、2 泡麵、3 飲品。</summary>
    public int Type { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public string? ImageUrl { get; set; }
}
