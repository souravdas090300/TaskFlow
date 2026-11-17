using FluentAssertions;
using TaskFlow.Application.Services;
using Xunit;

namespace TaskFlow.UnitTests.Services;

public class OrderPricingTests
{
    private static readonly OrderSettings Settings = new()
    {
        TaxRate = 0.18m,
        FreeShippingThreshold = 50m,
        ShippingCost = 5.99m,
        Currency = "EUR"
    };

    [Fact]
    public void Calculate_BelowThreshold_ChargesShipping()
    {
        var result = OrderPricing.Calculate(40m, Settings);

        result.Subtotal.Should().Be(40m);
        result.Tax.Should().Be(7.20m);
        result.Shipping.Should().Be(5.99m);
        result.Total.Should().Be(53.19m);
    }

    [Fact]
    public void Calculate_AtThreshold_FreeShipping()
    {
        // Exactly at the threshold qualifies for free shipping (>= comparison)
        var result = OrderPricing.Calculate(50m, Settings);

        result.Shipping.Should().Be(0m);
        result.Total.Should().Be(59.00m);
    }

    [Fact]
    public void Calculate_AboveThreshold_FreeShipping()
    {
        var result = OrderPricing.Calculate(100m, Settings);

        result.Shipping.Should().Be(0m);
        result.Total.Should().Be(118.00m);
    }

    [Fact]
    public void Defaults_AreSensible()
    {
        var defaults = new OrderSettings();
        defaults.Currency.Should().Be("EUR");
        defaults.TaxRate.Should().Be(0.18m);
    }

    [Fact]
    public void Calculate_ZeroSubtotal_NoShippingNoTax()
    {
        var result = OrderPricing.Calculate(0m, Settings);

        result.Shipping.Should().Be(5.99m);
        result.Tax.Should().Be(0m);
        result.Total.Should().Be(5.99m);
    }
}
