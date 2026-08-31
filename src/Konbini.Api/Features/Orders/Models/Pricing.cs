namespace Konbini.Api.Features.Orders.Models;

/// <summary>訂單金額規則：滿額免運，未滿收固定運費。</summary>
public static class Pricing
{
    public const int FreeDeliveryThreshold = 500;
    public const int DeliveryFee = 60;

    public static int CalculateDeliveryFee(int subtotal)
        => subtotal >= FreeDeliveryThreshold ? 0 : DeliveryFee;
}
