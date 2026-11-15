using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Repositories;
using TaskFlow.Domain.Constants;
using TaskFlow.Domain.Entities;

// PaymentResult already lives in Application. If it is in another namespace, add a using for it.

namespace TaskFlow.Application.Services;

public class CheckoutService
{
    private const string DateFormat = "MMMM dd, yyyy";

    private readonly ICartRepository _carts;
    private readonly IAddressLookup _addresses;
    private readonly IOrderRepository _orders;
    private readonly IStockRepository _stock;
    private readonly IProcessedWebhookEventRepository _events;
    private readonly IPaymentIssueRepository _issues;
    private readonly IUserDirectory _users;
    private readonly IEmailService _email;
    private readonly IUnitOfWork _uow;
    private readonly OrderSettings _settings;
    private readonly TimeProvider _time;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        ICartRepository carts, IAddressLookup addresses, IOrderRepository orders, IStockRepository stock,
        IProcessedWebhookEventRepository events, IPaymentIssueRepository issues, IUserDirectory users,
        IEmailService email, IUnitOfWork uow, IOptions<OrderSettings> settings,
        TimeProvider time, ILogger<CheckoutService> logger)
    {
        _carts = carts; _addresses = addresses; _orders = orders; _stock = stock; _events = events; _issues = issues;
        _users = users; _email = email; _uow = uow; _settings = settings.Value; _time = time; _logger = logger;
    }

    // ═══════════════════════ Create order ═══════════════════════

    public async Task<CreateOrderResult> CreateOrderAsync(Guid userId, Guid shippingAddressId, string paymentMethod)
    {
        var cart = await _carts.GetForCheckoutAsync(userId);
        if (cart is null || !cart.Items.Any())
            return CreateOrderResult.Fail(CreateOrderError.CartEmpty, "Your cart is empty.");

        if (!await _addresses.ExistsForUserAsync(shippingAddressId, userId))
            return CreateOrderResult.Fail(CreateOrderError.AddressNotFound, "Shipping address not found.");

        foreach (var item in cart.Items)
        {
            if (item.Product is null || !item.Product.IsActive)
                return CreateOrderResult.Fail(CreateOrderError.ProductUnavailable, "An item in your cart is no longer available.");
            if (item.Product.Stock < item.Quantity)
                return CreateOrderResult.Fail(CreateOrderError.InsufficientStock,
                    $"Only {item.Product.Stock} of '{item.Product.Name}' in stock.");
        }

        var totals = OrderPricing.Calculate(cart.Items.Sum(i => i.Product.Price * i.Quantity), _settings);
        var now = _time.GetUtcNow().UtcDateTime;

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderNumber = $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Status = OrderStatuses.Pending,
            PaymentMethod = paymentMethod,
            ShippingAddressId = shippingAddressId,
            Subtotal = totals.Subtotal,
            Tax = totals.Tax,
            ShippingCost = totals.Shipping,
            Total = totals.Total,
            CreatedAt = now,
            ConcurrencyToken = Guid.NewGuid()
        };

        var items = cart.Items.Select(ci => new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = ci.ProductId,
            ProductName = ci.Product.Name,
            ProductImageUrl = ci.Product.Images?.FirstOrDefault()?.ImageUrl,
            Quantity = ci.Quantity,
            UnitPrice = ci.Product.Price,
            TotalPrice = ci.Product.Price * ci.Quantity
        }).ToList();

        await _orders.AddWithItemsAsync(order, items);
        return CreateOrderResult.Ok(order.Id, order.OrderNumber);
    }

    // ═══════════════════════ Stripe payment ═══════════════════════

    public async Task<PaymentResult> ProcessStripePaymentAsync(Guid orderId, string paymentIntentId, string eventId)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var order = await _orders.GetByIdAsync(orderId);
        if (order is null)
        {
            _logger.LogError("Stripe event {EventId}: order {OrderId} not found", eventId, orderId);
            await _issues.AddIfNotExistsAsync(PaymentIssue.Create(null, null, paymentIntentId, eventId,
                PaymentIssueType.OrderNotFound, $"Stripe event referenced unknown order {orderId}", now));
            return PaymentResult.OrderNotFound;
        }

        var rejection = GetRejection(order, paymentIntentId, eventId);
        if (rejection is not null)
        {
            await _issues.AddIfNotExistsAsync(PaymentIssue.Create(order.Id, order.OrderNumber, paymentIntentId, eventId,
                rejection.Value.Type, rejection.Value.Details, now));
            return PaymentResult.Rejected;
        }

        var items = await _orders.GetItemsAsync(orderId);

        await using (var tx = await _uow.BeginTransactionAsync())
        {
            if (!await _events.TryMarkProcessedAsync(eventId))
                return PaymentResult.AlreadyProcessed;

            if (!await _orders.TryMarkPaidAsync(orderId, paymentIntentId, eventId, now))
            {
                await tx.CommitAsync();
                return PaymentResult.AlreadyProcessed;
            }

            if (await TryReserveStockAsync(items))
            {
                await _carts.ClearCartItemsAsync(order.UserId);
                await _uow.SaveChangesAsync();
                await tx.CommitAsync();
                await TrySendConfirmationAsync(order.UserId, order.OrderNumber, order.Total, now);
                return PaymentResult.Processed;
            }

            await tx.RollbackAsync();
        }

        return await FlagPaymentReviewAsync(order, paymentIntentId, eventId, now);
    }

    private (PaymentIssueType Type, string Details)? GetRejection(Order order, string paymentIntentId, string eventId)
    {
        if (order.Status == OrderStatuses.Paid && order.StripePaymentIntentId != paymentIntentId)
        {
            _logger.LogError(
                "Stripe event {EventId}: order {OrderNumber} already paid with DIFFERENT payment {OtherPayment}; {PaymentIntent} is a possible double charge. Refund needed.",
                eventId, order.OrderNumber, order.StripePaymentIntentId, paymentIntentId);
            return (PaymentIssueType.DoubleCharge,
                $"Order already paid with {order.StripePaymentIntentId}; second payment {paymentIntentId} must be refunded.");
        }

        if (order.Status is OrderStatuses.Cancelled or OrderStatuses.Shipped or OrderStatuses.Delivered)
        {
            _logger.LogError(
                "Stripe event {EventId}: payment {PaymentIntent} arrived for order {OrderNumber} in status {Status}. Refund may be needed.",
                eventId, paymentIntentId, order.OrderNumber, order.Status);
            return (PaymentIssueType.OrderClosed, $"Payment arrived for an order in status {order.Status}.");
        }

        return null;
    }

    private async Task<PaymentResult> FlagPaymentReviewAsync(Order order, string paymentIntentId, string eventId, DateTime now)
    {
        await using var tx = await _uow.BeginTransactionAsync();

        if (!await _events.TryMarkProcessedAsync(eventId))
            return PaymentResult.AlreadyProcessed;

        await _orders.MarkPaymentReviewAsync(order.Id, paymentIntentId);
        await _issues.AddIfNotExistsAsync(PaymentIssue.Create(order.Id, order.OrderNumber, paymentIntentId, eventId,
            PaymentIssueType.StockConflict, "Paid at Stripe but stock ran out. Refund or restock.", now));
        await tx.CommitAsync();

        _logger.LogError(
            "Order {OrderNumber} paid ({PaymentIntent}) but stock is insufficient. Flagged PaymentReview: refund or restock.",
            order.OrderNumber, paymentIntentId);
        return PaymentResult.StockConflict;
    }

    // ═══════════════════════ Cash on delivery ═══════════════════════

    public async Task<CashOnDeliveryResult> ProcessCashOnDeliveryAsync(Guid orderId, Guid userId)
    {
        var order = await _orders.GetByIdAsync(orderId);
        if (order is null || order.UserId != userId)
            return CashOnDeliveryResult.OrderNotFound;

        if (order.Status != OrderStatuses.Pending)
            return CashOnDeliveryResult.NotPending;

        if (order.PaymentMethod != PaymentMethodNames.CashOnDelivery)
            return CashOnDeliveryResult.NotCashOnDelivery;

        var items = await _orders.GetItemsAsync(orderId);
        var confirmedAt = _time.GetUtcNow().UtcDateTime;

        await using (var tx = await _uow.BeginTransactionAsync())
        {
            if (!await _orders.TryConfirmCashOnDeliveryAsync(orderId, userId, confirmedAt))
                return CashOnDeliveryResult.NotPending;

            if (!await TryReserveStockAsync(items))
            {
                await tx.RollbackAsync();
                _logger.LogWarning("COD order {OrderNumber}: not enough stock at confirmation", order.OrderNumber);
                return CashOnDeliveryResult.OutOfStock;
            }

            await _carts.ClearCartItemsAsync(userId);
            await _uow.SaveChangesAsync();
            await tx.CommitAsync();
        }

        await TrySendConfirmationAsync(userId, order.OrderNumber, order.Total, confirmedAt);
        return CashOnDeliveryResult.Confirmed;
    }

    // ═══════════════════════ helpers ═══════════════════════

    private async Task<bool> TryReserveStockAsync(IEnumerable<OrderItem> items)
    {
        foreach (var item in items.OrderBy(i => i.ProductId))
        {
            if (!await _stock.TryDecrementAsync(item.ProductId, item.Quantity))
                return false;
        }
        return true;
    }

    private async Task TrySendConfirmationAsync(Guid userId, string orderNumber, decimal total, DateTime dateUtc)
    {
        try
        {
            var contact = await _users.GetContactAsync(userId);
            if (contact is null) return;
            await _email.SendOrderConfirmationEmailAsync(contact.Email, contact.FullName, orderNumber, total, dateUtc.ToString(DateFormat));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email for {OrderNumber}", orderNumber);
        }
    }
}
