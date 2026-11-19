namespace Konbini.Api.Features.Products.Models;

/// <param name="Stock">庫存件數，0 表示售完。前端用來顯示售完、剩餘件數與數量上限。</param>
public record ProductDto(int Id, int Type, string Name, int Price, string? ImageUrl, int Stock);

public record ProductDetailDto(ProductDto Product, List<ProductDto> RelevantProducts);
