using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrderSettings:TaxRate"] = "0.19",  // MUTATED FROM 0.18
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
                    UnitPrice = 10.00m, 
                    Quantity = 2,
                    Product = new Product { Name = "Product1", Images = new List<ProductImage>() }
                },
                new CartItem 
                { 
                    Id = Guid.NewGuid(), 
                    UnitPrice = 20.00m, 
                    Quantity = 1,
                    Product = new Product { Name = "Product2", Images = new List<ProductImage>() }
                }
            }
        };

        mockCartRepo.Setup(r => r.GetByUserIdWithItemsAsync(It.IsAny<Guid>())).ReturnsAsync(cart);
        mockProductRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Product { Stock = 100 });

        var service = new CartService(mockCartRepo.Object, mockProductRepo.Object, config);

        // Act
        var result = await service.GetCartAsync(Guid.NewGuid());

        // Assert - Should use 0.19 (19% VAT), not 0.18
        result.Subtotal.Should().Be(40.00m);
        result.Tax.Should().Be(7.60m); // 40 * 0.19 = 7.60 (NOT 7.20)
        result.Shipping.Should().Be(5.99m);
        result.Total.Should().Be(53.59m); // 40 + 7.60 + 5.99 = 53.59 (NOT 53.19)
    }
}
