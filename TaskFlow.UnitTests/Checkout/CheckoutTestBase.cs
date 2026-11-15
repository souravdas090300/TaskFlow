using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TaskFlow.Application.Repositories;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Constants;
using TaskFlow.Domain.Entities;

namespace TaskFlow.UnitTests.Checkout;

/// <summary>v2: replaces the earlier CheckoutTestBase. Adds the payment-issues mock.</summary>
public abstract class CheckoutTestBase
{
    protected static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    protected static readonly Guid OrderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    protected static readonly Guid AddressId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    protected static readonly Guid Product1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    protected static readonly Guid Product2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

    protected readonly Mock<ICartRepository> Carts = new();
    protected readonly Mock<IAddressLookup> Addresses = new();
    protected readonly Mock<IOrderRepository> Orders = new();
    protected readonly Mock<IStockRepository> Stock = new();
    protected readonly Mock<IProcessedWebhookEventRepository> Events = new();
    protected readonly Mock<IPaymentIssueRepository> Issues = new();
    protected readonly Mock<IUserDirectory> Users = new();
    protected readonly Mock<IEmailService> Email = new();
    protected readonly Mock<IUnitOfWork> Uow = new();
    protected readonly Mock<IUnitOfWorkTransaction> Tx = new();

    protected readonly OrderSettings Settings = new() { TaxRate = 0.18m, FreeShippingThreshold = 50m, ShippingCost = 5.99m };

    protected Order? SavedOrder;
    protected List<OrderItem> SavedItems = new();

    protected CheckoutTestBase()
    {
        Uow.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(Tx.Object);
        Uow.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
        Tx.Setup(t => t.CommitAsync()).Returns(Task.CompletedTask);
        Tx.Setup(t => t.RollbackAsync()).Returns(Task.CompletedTask);

        Orders.Setup(o => o.AddWithItemsAsync(It.IsAny<Order>(), It.IsAny<IEnumerable<OrderItem>>()))
            .Callback<Order, IEnumerable<OrderItem>>((o, i) => { SavedOrder = o; SavedItems = i.ToList(); })
            .Returns(Task.CompletedTask);

        Carts.Setup(c => c.ClearCartItemsAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        Issues.Setup(i => i.AddIfNotExistsAsync(It.IsAny<PaymentIssue>())).Returns(Task.CompletedTask);
        Users.Setup(u => u.GetContactAsync(It.IsAny<Guid>())).ReturnsAsync(new UserContact("jane@example.com", "Jane Doe"));
        SetupEmailOk();
    }

    protected CheckoutService CreateSut() => new(
        Carts.Object, Addresses.Object, Orders.Object, Stock.Object, Events.Object, Issues.Object, Users.Object,
        Email.Object, Uow.Object, Options.Create(Settings), new FixedTimeProvider(), NullLogger<CheckoutService>.Instance);

    protected void SetupEmailOk() =>
        Email.Setup(e => e.SendOrderConfirmationEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

    protected void VerifyEmailSent(Times times) =>
        Email.Verify(e => e.SendOrderConfirmationEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()), times);

    protected void VerifyIssue(PaymentIssueType type, string eventId, Times times) =>
        Issues.Verify(i => i.AddIfNotExistsAsync(It.Is<PaymentIssue>(p => p.Type == type && p.EventId == eventId)), times);

    protected void StockAlwaysAvailable() =>
        Stock.Setup(s => s.TryDecrementAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);

    protected static Product MakeProduct(Guid id, decimal price, int stock, bool active = true) =>
        new() { Id = id, Name = $"Product {id.ToString()[^1]}", Price = price, Stock = stock, IsActive = active };

    protected static CartItem MakeCartItem(Product p, int qty, decimal? staleUnitPrice = null) =>
        new() { ProductId = p.Id, Product = p, Quantity = qty, UnitPrice = staleUnitPrice ?? p.Price };

    protected static Cart MakeCart(params CartItem[] items) => new() { UserId = UserId, Items = items.ToList() };

    protected void GivenCart(Cart? cart) => Carts.Setup(c => c.GetForCheckoutAsync(UserId)).ReturnsAsync(cart);
    protected void GivenOwnedAddress() => Addresses.Setup(a => a.ExistsForUserAsync(AddressId, UserId)).ReturnsAsync(true);

    protected static Order MakeOrder(string status = OrderStatuses.Pending, string method = "Stripe",
        Guid? owner = null, string? paymentIntent = null) => new()
    {
        Id = OrderId, UserId = owner ?? UserId, OrderNumber = "ORD-TEST", Status = status,
        PaymentMethod = method, Total = 118m, StripePaymentIntentId = paymentIntent
    };

    protected static List<OrderItem> MakeItems() => new()
    {
        new() { ProductId = Product2, ProductName = "Product 2", Quantity = 1 },
        new() { ProductId = Product1, ProductName = "Product 1", Quantity = 2 }
    };

    protected void GivenOrder(Order order)
    {
        Orders.Setup(o => o.GetByIdAsync(order.Id)).ReturnsAsync(order);
        Orders.Setup(o => o.GetItemsAsync(order.Id)).ReturnsAsync(MakeItems());
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);
    }
}
