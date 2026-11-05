using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskFlow.API.Services;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;
using Testcontainers.PostgreSql;

namespace TaskFlow.UnitTests.Integration;

public class PaymentIdempotencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private ApplicationDbContext? _context;
    private OrderService _orderService = null!;

    public PaymentIdempotencyTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("taskflow_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.MigrateAsync();

        _orderService = CreateOrderService(_context);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
        await _postgresContainer.DisposeAsync();
    }

    private OrderService CreateOrderService(ApplicationDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrderSettings:TaxRate"] = "0.10",
                ["OrderSettings:FreeShippingThreshold"] = "50",
                ["OrderSettings:ShippingCost"] = "5.99"
            })
            .Build();

        var mockEmailService = new Mock<IEmailService>();
        mockEmailService.Setup(x => x.SendOrderConfirmationEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var mockPaymentGateway = new Mock<IPaymentGateway>();

        return new OrderService(context, configuration, mockEmailService.Object, mockPaymentGateway.Object);
    }

    [Fact]
    public async Task ProcessStripePaymentAsync_SameEventIdTwice_IsIdempotent()
    {
        // Arrange
        // Create test data
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Test Category",
            Slug = "test-category",
            DisplayOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context!.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test",
            Price = 100.00m,
            Stock = 10,
            Slug = "test-product",
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User",
            Name = "Test User",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrderNumber = "ORD-TEST-001",
            Status = "Pending",
            PaymentMethod = "CreditCard",
            Subtotal = 100.00m,
            Tax = 10.00m,
            ShippingCost = 5.99m,
            Total = 115.99m,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };
        _context.Orders.Add(order);

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = product.Id,
            ProductName = "Test Product",
            Quantity = 1,
            UnitPrice = 100.00m,
            TotalPrice = 100.00m
        };
        _context.OrderItems.Add(orderItem);

        await _context.SaveChangesAsync();

        const string eventId = "evt_test_12345";
        const string paymentIntentId = "pi_test_12345";

        // Act - Process the same payment twice
        var firstResult = await _orderService.ProcessStripePaymentAsync(order.Id, paymentIntentId, eventId);

        // Clear change tracker and recreate service to allow second call with same EventId
        _context.ChangeTracker.Clear();
        _orderService = CreateOrderService(_context);

        var secondResult = await _orderService.ProcessStripePaymentAsync(order.Id, paymentIntentId, eventId);

        // Assert
        Assert.True(firstResult); // First payment should succeed
        Assert.True(secondResult); // Second payment should return true (idempotent)

        // Verify order was marked paid only once
        _context.ChangeTracker.Clear();
        var paidOrder = await _context.Orders.FindAsync(order.Id);
        Assert.Equal("Paid", paidOrder?.Status);
        Assert.Equal(paymentIntentId, paidOrder?.StripePaymentIntentId);
        Assert.Equal(eventId, paidOrder?.StripeEventId);

        // Verify stock was decremented only once
        var stockProduct = await _context.Products.FindAsync(product.Id);
        Assert.Equal(9, stockProduct?.Stock); // Started at 10, decremented once

        // Verify exactly one processed event record exists
        var eventCount = await _context.ProcessedWebhookEvents.CountAsync(e => e.EventId == eventId);
        Assert.Equal(1, eventCount);
    }
}
