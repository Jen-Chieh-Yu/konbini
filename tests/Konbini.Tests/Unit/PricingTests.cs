using Konbini.Api.Features.Orders.Models;
using Xunit;

namespace Konbini.Tests.Unit;

public class PricingTests
{
    [Theory]
    [InlineData(0, 60)]
    [InlineData(499, 60)]
    [InlineData(500, 0)]   // 滿 500 免運（含 500）
    [InlineData(1000, 0)]
    public void CalculateDeliveryFee_FollowsThresholdRule(int subtotal, int expectedFee)
    {
        Assert.Equal(expectedFee, Pricing.CalculateDeliveryFee(subtotal));
    }
}
