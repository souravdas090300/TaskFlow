using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TaskFlow.API.Services;
using TaskFlow.Application.Repositories;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Repositories;
using Testcontainers.PostgreSql;

namespace TaskFlow.UnitTests.Integration;

public class PaymentIdempotencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private ApplicationDbContext? _context;
    private OrderService _orderService = null!;
    private readonly Mock<IEmailService> _mockEmailService = new();

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

        _mockEmailService.Setup(x => x.SendOrderConfirmationEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var orderRepository = new OrderRepository(context);
        var eventRepository = new ProcessedWebhookEventRepository(context);
        var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<OrderService>>();

        return new OrderService(context, configuration, _mockEmailService.Object, orderRepository, eventRepository, logger.Object);
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

    [Fact]
    public async Task ProcessStripePaymentAsync_ConcurrentStockDecrement_LastItem_OnlyOneSucceeds()
    {
        // Arrange - Create product with stock=1
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
            Name = "Last Item",
            Description = "Test",
            Price = 100.00m,
            Stock = 1, // Only 1 in stock
            Slug = "last-item",
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

        // Create two orders for the same product
        var order1 = new Order
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
        _context.Orders.Add(order1);

        var order2 = new Order
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrderNumber = "ORD-TEST-002",
            Status = "Pending",
            PaymentMethod = "CreditCard",
            Subtotal = 100.00m,
            Tax = 10.00m,
            ShippingCost = 5.99m,
            Total = 115.99m,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };
        _context.Orders.Add(order2);

        var orderItem1 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order1.Id,
            ProductId = product.Id,
            ProductName = "Last Item",
            Quantity = 1,
            UnitPrice = 100.00m,
            TotalPrice = 100.00m
        };
        _context.OrderItems.Add(orderItem1);

        var orderItem2 = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order2.Id,
            ProductId = product.Id,
            ProductName = "Last Item",
            Quantity = 1,
            UnitPrice = 100.00m,
            TotalPrice = 100.00m
        };
        _context.OrderItems.Add(orderItem2);

        await _context.SaveChangesAsync();

        const string eventId1 = "evt_test_concurrent_1";
        const string eventId2 = "evt_test_concurrent_2";
        const string paymentIntentId1 = "pi_test_concurrent_1";
        const string paymentIntentId2 = "pi_test_concurrent_2";

        // Act - Process both payments concurrently with separate DbContext instances
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        using var context1 = new ApplicationDbContext(options);
        using var context2 = new ApplicationDbContext(options);

        var service1 = CreateOrderService(context1);
        var service2 = CreateOrderService(context2);

        var results = await Task.WhenAll(
            service1.ProcessStripePaymentAsync(order1.Id, paymentIntentId1, eventId1),
            service2.ProcessStripePaymentAsync(order2.Id, paymentIntentId2, eventId2)
        );

        // Assert - Both should return true (webhook 200) - one Paid, one PaymentReview
        Assert.True(results[0]);
        Assert.True(results[1]);

        // Verify stock is now 0
        _context.ChangeTracker.Clear();
        var stockProduct = await _context.Products.FindAsync(product.Id);
        Assert.Equal(0, stockProduct?.Stock);

        // Verify exactly one order is Paid and one is PaymentReview
        _context.ChangeTracker.Clear();
        var paidOrders = await _context.Orders
            .Where(o => o.Status == "Paid" && (o.Id == order1.Id || o.Id == order2.Id))
            .CountAsync();
        var reviewOrders = await _context.Orders
            .Where(o => o.Status == "PaymentReview" && (o.Id == order1.Id || o.Id == order2.Id))
            .CountAsync();
        Assert.Equal(1, paidOrders);
        Assert.Equal(1, reviewOrders);
    }

    [Fact]
    public async Task ProcessStripePaymentAsync_StockRunsOut_SetsPaymentReview_Returns200()
    {
        // Arrange - Create two products: A with stock 5, B with stock 0
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

        // Product A: GUID sorts first, has stock 5
        var productA = new Product
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Product A",
            Description = "Test",
            Price = 50.00m,
            Stock = 5,
            Slug = "product-a",
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(productA);

        // Product B: GUID sorts second, has stock 0
        var productB = new Product
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Name = "Product B",
            Description = "Test",
            Price = 50.00m,
            Stock = 0, // Out of stock
            Slug = "product-b",
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(productB);

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
            OrderNumber = "ORD-TEST-OUTOFSTOCK",
            Status = "Pending",
            PaymentMethod = "CreditCard",
            Subtotal = 150.00m,
            Tax = 15.00m,
            ShippingCost = 5.99m,
            Total = 170.99m,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };
        _context.Orders.Add(order);

        // Line 1: Product A, quantity 2 (will succeed)
        var orderItemA = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = productA.Id,
            ProductName = "Product A",
            Quantity = 2,
            UnitPrice = 50.00m,
            TotalPrice = 100.00m
        };
        _context.OrderItems.Add(orderItemA);

        // Line 2: Product B, quantity 1 (will fail)
        var orderItemB = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = productB.Id,
            ProductName = "Product B",
            Quantity = 1,
            UnitPrice = 50.00m,
            TotalPrice = 50.00m
        };
        _context.OrderItems.Add(orderItemB);

        // Add cart items to verify they remain intact
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Carts.Add(cart);

        var cartItemA = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = productA.Id,
            Quantity = 1
        };
        _context.CartItems.Add(cartItemA);

        var cartItemB = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = productB.Id,
            Quantity = 1
        };
        _context.CartItems.Add(cartItemB);

        await _context.SaveChangesAsync();

        const string eventId = "evt_test_outofstock";
        const string paymentIntentId = "pi_test_outofstock";

        // Track email calls
        var emailCalls = 0;
        _mockEmailService.Setup(x => x.SendOrderConfirmationEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<string>()))
            .Callback(() => emailCalls++)
            .Returns(Task.CompletedTask);

        // Act - Payment arrives but stock is out on line 2
        var result = await _orderService.ProcessStripePaymentAsync(order.Id, paymentIntentId, eventId);

        // Assert - Webhook returns 200 (true means success, stripe doesn't retry)
        Assert.True(result);

        // Verify order status is PaymentReview (not Paid, not Pending)
        _context.ChangeTracker.Clear();
        var orderStatus = await _context.Orders.FindAsync(order.Id);
        Assert.Equal("PaymentReview", orderStatus?.Status);

        // Verify PaidAt is null (order was never committed as Paid)
        Assert.Null(orderStatus?.PaidAt);

        // Verify payment intent ID is stored
        Assert.Equal(paymentIntentId, orderStatus?.StripePaymentIntentId);

        // Product A stock should still be 5 (rollback restored it)
        var stockA = await _context.Products.FindAsync(productA.Id);
        Assert.Equal(5, stockA?.Stock);

        // Product B stock should still be 0
        var stockB = await _context.Products.FindAsync(productB.Id);
        Assert.Equal(0, stockB?.Stock);

        // Verify cart items are still there (not cleared)
        var cartItems = await _context.CartItems
            .Where(ci => ci.CartId == cart.Id)
            .CountAsync();
        Assert.Equal(2, cartItems);

        // Verify exactly one event row exists
        var eventCount = await _context.ProcessedWebhookEvents
            .Where(e => e.EventId == eventId)
            .CountAsync();
        Assert.Equal(1, eventCount);

        // Verify no email was sent
        Assert.Equal(0, emailCalls);
    }

    [Fact]
    public async Task ProcessStripePaymentAsync_ConcurrentDuplicateEvent_Idempotent()
    {
        // Arrange - Create test data
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
            OrderNumber = "ORD-TEST-CONCURRENT",
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

        const string eventId = "evt_test_concurrent";
        const string paymentIntentId = "pi_test_concurrent";

        // Act - Process the same event concurrently with separate DbContext instances
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        using var context1 = new ApplicationDbContext(options);
        using var context2 = new ApplicationDbContext(options);

        var service1 = CreateOrderService(context1);
        var service2 = CreateOrderService(context2);

        var results = await Task.WhenAll(
            service1.ProcessStripePaymentAsync(order.Id, paymentIntentId, eventId),
            service2.ProcessStripePaymentAsync(order.Id, paymentIntentId, eventId)
        );

        // Assert - Both should return true (webhook 200)
        Assert.True(results[0]);
        Assert.True(results[1]);

        // Verify order was marked paid only once
        _context.ChangeTracker.Clear();
        var paidOrder = await _context.Orders.FindAsync(order.Id);
        Assert.Equal("Paid", paidOrder?.Status);
        Assert.Equal(paymentIntentId, paidOrder?.StripePaymentIntentId);

        // Verify stock was decremented only once
        var stockProduct = await _context.Products.FindAsync(product.Id);
        Assert.Equal(9, stockProduct?.Stock); // Started at 10, decremented once

        // Verify exactly one processed event record exists
        var eventCount = await _context.ProcessedWebhookEvents
            .Where(e => e.EventId == eventId)
            .CountAsync();
        Assert.Equal(1, eventCount);

        // Verify exactly one email was sent
        _mockEmailService.Verify(
            x => x.SendOrderConfirmationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessStripePaymentAsync_TwoDifferentEventsForOneOrder_StockDecrementedOnce()
    {
        // Arrange - Create test data
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
            OrderNumber = "ORD-TEST-TWOEVENTS",
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

        const string eventId1 = "evt_test_first";
        const string eventId2 = "evt_test_second";
        const string paymentIntentId = "pi_test_twoevents";

        // Act - Process two different events for the same order
        var firstResult = await _orderService.ProcessStripePaymentAsync(order.Id, paymentIntentId, eventId1);

        _context.ChangeTracker.Clear();
        _orderService = CreateOrderService(_context);

        var secondResult = await _orderService.ProcessStripePaymentAsync(order.Id, paymentIntentId, eventId2);

        // Assert - Both should return true (webhook 200)
        Assert.True(firstResult);
        Assert.True(secondResult);

        // Verify order was marked paid only once
        _context.ChangeTracker.Clear();
        var paidOrder = await _context.Orders.FindAsync(order.Id);
        Assert.Equal("Paid", paidOrder?.Status);
        Assert.Equal(paymentIntentId, paidOrder?.StripePaymentIntentId);

        // Verify stock was decremented only once
        var stockProduct = await _context.Products.FindAsync(product.Id);
        Assert.Equal(9, stockProduct?.Stock); // Started at 10, decremented once

        // Verify two processed event records exist
        var eventCount = await _context.ProcessedWebhookEvents
            .Where(e => e.EventId == eventId1 || e.EventId == eventId2)
            .CountAsync();
        Assert.Equal(2, eventCount);

        // Verify exactly one email was sent
        _mockEmailService.Verify(
            x => x.SendOrderConfirmationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessStripePaymentAsync_CancelledOrder_Rejected()
    {
        // Arrange - Create test data
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
            OrderNumber = "ORD-TEST-CANCELLED",
            Status = "Cancelled",
            PaymentMethod = "CreditCard",
            Subtotal = 100.00m,
            Tax = 10.00m,
            ShippingCost = 5.99m,
            Total = 115.99m,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };
        _context.Orders.Add(order);

        await _context.SaveChangesAsync();

        const string eventId = "evt_test_cancelled";
        const string paymentIntentId = "pi_test_cancelled";

        // Act - Process payment for cancelled order
        var result = await _orderService.ProcessStripePaymentAsync(order.Id, paymentIntentId, eventId);

        // Assert - Should return false (rejected)
        Assert.False(result);

        // Verify order status is still Cancelled
        _context.ChangeTracker.Clear();
        var orderStatus = await _context.Orders.FindAsync(order.Id);
        Assert.Equal("Cancelled", orderStatus?.Status);

        // Verify stock was not decremented
        var stockProduct = await _context.Products.FindAsync(product.Id);
        Assert.Equal(10, stockProduct?.Stock);

        // Verify no event row was created
        var eventExists = await _context.ProcessedWebhookEvents
            .AnyAsync(e => e.EventId == eventId);
        Assert.False(eventExists);

        // Verify no email was sent
        _mockEmailService.Verify(
            x => x.SendOrderConfirmationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>()),
            Times.Never);
    }
}
