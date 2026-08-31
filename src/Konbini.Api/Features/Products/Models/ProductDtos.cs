namespace Konbini.Api.Features.Products.Models;

public record ProductDto(int Id, int Type, string Name, int Price, string? ImageUrl);

public record ProductDetailDto(ProductDto Product, List<ProductDto> RelevantProducts);
