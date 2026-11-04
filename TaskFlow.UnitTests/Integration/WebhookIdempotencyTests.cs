using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TaskFlow.API.Services;
using TaskFlow.Application.Repositories;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskFlow.UnitTests.Integration;

public class WebhookIdempotencyTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private ApplicationDbContext? _context;
    private Mock<IEmailService>? _mockEmailService;
    private Mock<IPaymentGateway>? _mockPaymentGateway;
    private OrderService? _orderService;
    private Guid _testUserId;
    private Guid _testProductId;
    private Guid _testOrderId;
    private const string TestEventId = "evt_test_12345";
    private const string TestPaymentIntentId = "pi_test_67890";

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("taskflow_test")
            .WithUsername("taskflow")
            .WithPassword("test_password")
            .Build();

        await _postgresContainer.StartAsync();

        // Create DbContext with Testcontainers connection string
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Run migrations
        await _context.Database.MigrateAsync();

        // Setup mocks
        _mockEmailService = new Mock<IEmailService>();
        _mockPaymentGateway = new Mock<IPaymentGateway>();

        // Create test data
        await SetupTestData();

        // Create OrderService
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrderSettings:TaxRate"] = "0.18",
                ["OrderSettings:FreeShippingThreshold"] = "50",
                ["OrderSettings:ShippingCost"] = "5.99"
            })
            .Build();

        _orderService = new OrderService(_context, config, _mockEmailService!.Object, _mockPaymentGateway!.Object);
    }

    private async Task SetupTestData()
    {
        // Create test user
        _testUserId = Guid.NewGuid();
        var user = new User
        {
            Id = _testUserId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        };
        _context.Users.Add(user);

        // Create test product with 10 units in stock
        _testProductId = Guid.NewGuid();
        var product = new Product
        {
            Id = _testProductId,
            Name = "Test Product",
            Description = "Test",
            Price = 100.00m,
            Stock = 10,
            Slug = "test-product",
            CategoryId = Guid.NewGuid()
        };
        _context.Products.Add(product);

        // Create pending order with 1 item
        _testOrderId = Guid.NewGuid();
        var order = new Order
        {
            Id = _testOrderId,
            UserId = _testUserId,
            OrderNumber = "ORD-001",
            Status = "Pending",
            Subtotal = 100.00m,
            Tax = 18.00m,
            ShippingCost = 5.99m,
            Total = 123.99m,
            PaymentMethod = "Stripe"
        };
        _context.Orders.Add(order);

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = _testOrderId,
            ProductId = _testProductId,
            Quantity = 1,
            UnitPrice = 100.00m
        };
        _context.OrderItems.Add(orderItem);

        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessStripePaymentAsync_SameEventIdTwice_IsIdempotent()
    {
        // Arrange - Initial state
        var initialOrder = await _context.Orders.FindAsync(_testOrderId);
        var initialProduct = await _context.Products.FindAsync(_testProductId);
        initialOrder!.Status.Should().Be("Pending");
        initialProduct!.Stock.Should().Be(10);

        // Act - First call
        var firstResult = await _orderService!.ProcessStripePaymentAsync(_testOrderId, TestPaymentIntentId, TestEventId);

        // Assert - First call succeeds
        firstResult.Should().BeTrue();

        var afterFirstOrder = await _context.Orders.FindAsync(_testOrderId);
        var afterFirstProduct = await _context.Products.FindAsync(_testProductId);
        var afterFirstEvents = await _context.ProcessedWebhookEvents.ToListAsync();

        afterFirstOrder!.Status.Should().Be("Paid");
        afterFirstOrder.StripeEventId.Should().Be(TestEventId);
        afterFirstOrder.StripePaymentIntentId.Should().Be(TestPaymentIntentId);
        afterFirstProduct!.Stock.Should().Be(9); // 10 - 1 = 9
        afterFirstEvents.Should().HaveCount(1);
        afterFirstEvents.Should().ContainSingle(e => e.EventId == TestEventId);

        // Act - Second call with SAME EventId (idempotency test)
        var secondResult = await _orderService.ProcessStripePaymentAsync(_testOrderId, TestPaymentIntentId, TestEventId);

        // Assert - Second call returns true (idempotent success)
        secondResult.Should().BeTrue();

        var afterSecondOrder = await _context.Orders.FindAsync(_testOrderId);
        var afterSecondProduct = await _context.Products.FindAsync(_testProductId);
        var afterSecondEvents = await _context.ProcessedWebhookEvents.ToListAsync();

        // Order should still be paid (not "Paid" twice or changed)
        afterSecondOrder!.Status.Should().Be("Paid");
        afterSecondOrder.StripeEventId.Should().Be(TestEventId);
        afterSecondOrder.StripePaymentIntentId.Should().Be(TestPaymentIntentId);

        // Stock should be 9 (not decremented again to 8)
        afterSecondProduct!.Stock.Should().Be(9);

        // Still exactly one ProcessedWebhookEvent row
        afterSecondEvents.Should().HaveCount(1);
        afterSecondEvents.Should().ContainSingle(e => e.EventId == TestEventId);
    }

    [Fact]
    public async Task ProcessStripePaymentAsync_ConcurrentSameEventId_IsIdempotent()
    {
        // Arrange - Initial state
        var initialOrder = await _context.Orders.FindAsync(_testOrderId);
        var initialProduct = await _context.Products.FindAsync(_testProductId);
        initialOrder!.Status.Should().Be("Pending");
        initialProduct!.Stock.Should().Be(10);

        // Act - Call concurrently with same EventId
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 2; i++)
        {
            tasks.Add(_orderService!.ProcessStripePaymentAsync(_testOrderId, TestPaymentIntentId, TestEventId));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Both calls return true (idempotent)
        results.Should().AllBeEquivalentTo(true);

        var finalOrder = await _context.Orders.FindAsync(_testOrderId);
        var finalProduct = await _context.Products.FindAsync(_testProductId);
        var finalEvents = await _context.ProcessedWebhookEvents.ToListAsync();

        // Order paid once
        finalOrder!.Status.Should().Be("Paid");
        finalOrder.StripeEventId.Should().Be(TestEventId);

        // Stock decremented once (not twice)
        finalProduct!.Stock.Should().Be(9);

        // Exactly one ProcessedWebhookEvent row
        finalEvents.Should().HaveCount(1);
        finalEvents.Should().ContainSingle(e => e.EventId == TestEventId);
    }
}
