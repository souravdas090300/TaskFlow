using FluentAssertions;
using Moq;
using TaskFlow.Application;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Constants;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.UnitTests.Checkout;

public class CheckoutServiceTests : CheckoutTestBase
{
    [Fact]
    public async Task CreateOrder_EmptyCart_ReturnsCartEmpty()
    {
        GivenCart(null);

        var result = await CreateSut().CreateOrderAsync(UserId, AddressId, PaymentMethodNames.Stripe);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CreateOrderError.CartEmpty);
    }

    [Fact]
    public async Task CreateOrder_AddressNotOwned_ReturnsAddressNotFound()
    {
        var product = MakeProduct(Product1, 10m, 5);
        GivenCart(MakeCart(MakeCartItem(product, 1)));
        // Addresses.ExistsForUserAsync not set up -> false

        var result = await CreateSut().CreateOrderAsync(UserId, AddressId, PaymentMethodNames.Stripe);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CreateOrderError.AddressNotFound);
    }

    [Fact]
    public async Task CreateOrder_InactiveProduct_ReturnsProductUnavailable()
    {
        var product = MakeProduct(Product1, 10m, 5, active: false);
        GivenCart(MakeCart(MakeCartItem(product, 1)));
        GivenOwnedAddress();

        var result = await CreateSut().CreateOrderAsync(UserId, AddressId, PaymentMethodNames.Stripe);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CreateOrderError.ProductUnavailable);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_ReturnsInsufficientStock()
    {
        var product = MakeProduct(Product1, 10m, 1);
        GivenCart(MakeCart(MakeCartItem(product, 2)));
        GivenOwnedAddress();

        var result = await CreateSut().CreateOrderAsync(UserId, AddressId, PaymentMethodNames.Stripe);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CreateOrderError.InsufficientStock);
    }

    [Fact]
    public async Task CreateOrder_ValidCart_CreatesOrderWithPriceSnapshotsAndTotals()
    {
        var p1 = MakeProduct(Product1, 10m, 5);
        var p2 = MakeProduct(Product2, 20m, 5);
        GivenCart(MakeCart(MakeCartItem(p1, 2), MakeCartItem(p2, 1)));
        GivenOwnedAddress();

        var result = await CreateSut().CreateOrderAsync(UserId, AddressId, PaymentMethodNames.Stripe);

        result.IsSuccess.Should().BeTrue();
        result.OrderId.Should().Be(SavedOrder!.Id);
        result.OrderNumber.Should().StartWith("ORD-");

        SavedOrder!.Status.Should().Be(OrderStatuses.Pending);
        SavedOrder.PaymentMethod.Should().Be(PaymentMethodNames.Stripe);
        SavedOrder.ShippingAddressId.Should().Be(AddressId);
        SavedOrder.UserId.Should().Be(UserId);

        // subtotal 40 -> tax 7.20 (18%), shipping 5.99 (< 50 threshold), total 53.19
        SavedOrder.Subtotal.Should().Be(40m);
        SavedOrder.Tax.Should().Be(7.20m);
        SavedOrder.ShippingCost.Should().Be(5.99m);
        SavedOrder.Total.Should().Be(53.19m);

        SavedItems.Should().HaveCount(2);
        SavedItems.Should().OnlyContain(i => i.OrderId == SavedOrder.Id);
        SavedItems.Should().OnlyContain(i => i.UnitPrice == i.TotalPrice / i.Quantity);
    }

    [Fact]
    public async Task Cod_OrderNotFound_ReturnsOrderNotFound()
    {
        Orders.Setup(o => o.GetByIdAsync(OrderId)).ReturnsAsync((Order?)null);

        var result = await CreateSut().ProcessCashOnDeliveryAsync(OrderId, UserId);

        result.Should().Be(CashOnDeliveryResult.OrderNotFound);
    }

    [Fact]
    public async Task Cod_OrderOwnedBySomeoneElse_ReturnsOrderNotFound()
    {
        var otherUser = Guid.NewGuid();
        GivenOrder(MakeOrder(method: PaymentMethodNames.CashOnDelivery, owner: otherUser));

        var result = await CreateSut().ProcessCashOnDeliveryAsync(OrderId, UserId);

        result.Should().Be(CashOnDeliveryResult.OrderNotFound);
    }

    [Fact]
    public async Task Cod_NotPending_ReturnsNotPending()
    {
        GivenOrder(MakeOrder(OrderStatuses.Paid, method: PaymentMethodNames.CashOnDelivery));

        var result = await CreateSut().ProcessCashOnDeliveryAsync(OrderId, UserId);

        result.Should().Be(CashOnDeliveryResult.NotPending);
    }

    [Fact]
    public async Task Cod_StripeOrder_ReturnsNotCashOnDelivery()
    {
        GivenOrder(MakeOrder(method: PaymentMethodNames.Stripe));

        var result = await CreateSut().ProcessCashOnDeliveryAsync(OrderId, UserId);

        result.Should().Be(CashOnDeliveryResult.NotCashOnDelivery);
    }

    [Fact]
    public async Task Cod_OutOfStock_RollsBackAndReturnsOutOfStock()
    {
        GivenOrder(MakeOrder(method: PaymentMethodNames.CashOnDelivery));
        Orders.Setup(o => o.TryConfirmCashOnDeliveryAsync(OrderId, UserId, It.IsAny<DateTime>())).ReturnsAsync(true);
        Stock.Setup(s => s.TryDecrementAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(false);

        var result = await CreateSut().ProcessCashOnDeliveryAsync(OrderId, UserId);

        result.Should().Be(CashOnDeliveryResult.OutOfStock);
        Tx.Verify(t => t.RollbackAsync(), Times.Once);
        VerifyEmailSent(Times.Never());
    }

    [Fact]
    public async Task Cod_ValidOrder_ConfirmsDecrementsStockAndSendsEmail()
    {
        GivenOrder(MakeOrder(method: PaymentMethodNames.CashOnDelivery));
        Orders.Setup(o => o.TryConfirmCashOnDeliveryAsync(OrderId, UserId, It.IsAny<DateTime>())).ReturnsAsync(true);
        StockAlwaysAvailable();

        var result = await CreateSut().ProcessCashOnDeliveryAsync(OrderId, UserId);

        result.Should().Be(CashOnDeliveryResult.Confirmed);
        Orders.Verify(o => o.TryConfirmCashOnDeliveryAsync(OrderId, UserId, It.IsAny<DateTime>()), Times.Once);
        Stock.Verify(s => s.TryDecrementAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Exactly(2));
        Carts.Verify(c => c.ClearCartItemsAsync(UserId), Times.Once);
        VerifyEmailSent(Times.Once());
    }
}
