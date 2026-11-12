namespace Konbini.Api.Features.Orders.Models;

public record CreateOrderItemDto(int ProductId, int Quantity);

public record CreateOrderRequest(
    List<CreateOrderItemDto> Items,
    string ContactName,
    string ContactPhone,
    int DeliveryMethod,
    int CityCode,
    int DistrictCode,
    string StreetAddress,
    string? Memo);

public record OrderItemDto(
    int ProductId, string ProductName, int UnitPrice, int Quantity, int Subtotal, string? ImageUrl);

public record OrderDto(
    int Id,
    int Subtotal,
    int DeliveryFee,
    int Total,
    string ContactName,
    string ContactPhone,
    string DeliveryAddress,
    string? Memo,
    DateTime CreatedAt,
    List<OrderItemDto> Items);
