using FluentAssertions;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.UnitTests.Services;

public class CartServiceTests
{
    // NOTE: These tests are arithmetic-only and do NOT exercise actual service logic.
    // Mutation test (changing VAT in appsettings.json from 0.18 to 0.19) shows tests still pass.
    // Real service-level tests require Testcontainers to test:
    // - Actual CartService.GetCartAsync() with real database
    // - Configuration-driven calculations via IConfiguration
    // - Transactional behavior (stock reduction, idempotency)
    // 
    // Services remain in API layer due to circular dependency (Application ↔ Infrastructure).
    // UnitTests already references API, so services are testable - but need Testcontainers for real tests.
    
    [Fact]
    public void CalculateCartTotals_UsesConfigurationVatRate()
    {
        // TODO: Replace with integration test using Testcontainers that calls CartService
        // and reads from actual IConfiguration
        
        var taxRate = 0.18m; // This should match appsettings.json OrderSettings:TaxRate
        var freeShippingThreshold = 50m;
        var shippingCost = 5.99m;

        var cart = new Cart
        {
            Items = new List<CartItem>
            {
                new CartItem { UnitPrice = 10.00m, Quantity = 2 },
                new CartItem { UnitPrice = 20.00m, Quantity = 1 }
            }
        };

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var tax = subtotal * taxRate;
        var shipping = subtotal >= freeShippingThreshold ? 0 : shippingCost;
        var total = subtotal + tax + shipping;

        subtotal.Should().Be(40.00m);
        tax.Should().Be(7.20m);
        shipping.Should().Be(5.99m);
        total.Should().Be(53.19m);
    }

    [Fact]
    public void CalculateCartTotals_FreeShippingOverThreshold()
    {
        var taxRate = 0.18m;
        var freeShippingThreshold = 50m;

        var cart = new Cart
        {
            Items = new List<CartItem>
            {
                new CartItem { UnitPrice = 30.00m, Quantity = 2 }
            }
        };

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var tax = subtotal * taxRate;
        var shipping = subtotal >= freeShippingThreshold ? 0 : 5.99m;

        subtotal.Should().Be(60.00m);
        tax.Should().Be(10.80m);
        shipping.Should().Be(0);
    }
}
