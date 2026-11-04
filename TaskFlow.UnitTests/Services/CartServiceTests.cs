using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskFlow.Application.Repositories;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.UnitTests.Services;

public class CartServiceTests
{
    [Fact]
    public async Task CalculateCartTotals_UsesConfigurationVatRate()
    {
        // Arrange - Configuration with MUTATED VAT rate (0.19 instead of 0.18)
        // This is a MUTATION TEST: changing config should cause test to fail if it's not reading config
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrderSettings:TaxRate"] = "0.19",  // MUTATED FROM 0.18 (appsettings.json)
                ["OrderSettings:FreeShippingThreshold"] = "999",  // MUTATED FROM 50 (free shipping won't apply)
                ["OrderSettings:ShippingCost"] = "0"  // MUTATED FROM 5.99 (shipping won't add cost)
            })
            .Build();

        var mockCartRepo = new Mock<ICartRepository>();
        var mockProductRepo = new Mock<IProductRepository>();

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Items = new List<CartItem>
            {
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    UnitPrice = 100.00m,
                    Quantity = 1,
                    Product = new Product { Name = "Product1", Images = new List<ProductImage>() }
                }
            }
        };

        mockCartRepo.Setup(r => r.GetByUserIdWithItemsAsync(It.IsAny<Guid>())).ReturnsAsync(cart);
        mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Product { Stock = 100 });

        var service = new CartService(mockCartRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await service.GetCartAsync(Guid.NewGuid());

        // Assert - MUTATION: Test must use mutated config values
        // If test uses hardcoded 0.18/50/5.99, these assertions will FAIL
        result.Subtotal.Should().Be(100.00m);
        result.Tax.Should().Be(19.00m); // 100 * 0.19 = 19.00 (NOT 18.00)
        result.Shipping.Should().Be(0m); // Threshold is 999, so shipping applies: 0 (NOT 5.99 or 0)
        result.Total.Should().Be(119.00m); // 100 + 19 + 0 = 119.00 (NOT 118.00 or 123.99)
    }

    [Fact]
    public async Task CalculateCartTotals_FreeShippingOverThreshold()
    {
        // Arrange - Normal config (like appsettings.json)
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrderSettings:TaxRate"] = "0.18",
                ["OrderSettings:FreeShippingThreshold"] = "50",
                ["OrderSettings:ShippingCost"] = "5.99"
            })
            .Build();

        var mockCartRepo = new Mock<ICartRepository>();
        var mockProductRepo = new Mock<IProductRepository>();

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Items = new List<CartItem>
            {
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    UnitPrice = 30.00m,
                    Quantity = 2,
                    Product = new Product { Name = "Product1", Images = new List<ProductImage>() }
                }
            }
        };

        mockCartRepo.Setup(r => r.GetByUserIdWithItemsAsync(It.IsAny<Guid>())).ReturnsAsync(cart);
        mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Product { Stock = 100 });

        var service = new CartService(mockCartRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await service.GetCartAsync(Guid.NewGuid());

        // Assert - Free shipping over 50 threshold
        result.Subtotal.Should().Be(60.00m);
        result.Tax.Should().Be(10.80m); // 60 * 0.18
        result.Shipping.Should().Be(0m); // Subtotal >= 50, free shipping
        result.Total.Should().Be(70.80m);
    }
}
