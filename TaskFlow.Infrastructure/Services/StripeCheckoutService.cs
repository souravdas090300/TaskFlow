using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using TaskFlow.Application.Repositories;
using TaskFlow.Domain.Constants;
namespace TaskFlow.Infrastructure.Services;

public sealed record CheckoutSessionResult(bool Ok, string? Url, string? Error);

/// <summary>
/// Creates Stripe Checkout Sessions for pending orders. The charged amount is
/// always derived from the order in the database — never from client input.
/// </summary>
public sealed class StripeCheckoutService
{
    private readonly IOrderRepository _orders;
    private readonly IUserDirectory _users;
    private readonly string _publicUrl;

    public StripeCheckoutService(IOrderRepository orders, IUserDirectory users, IConfiguration config)
    {
        _orders = orders;
        _users = users;
        _publicUrl = (config["App:PublicUrl"] ?? throw new InvalidOperationException("App:PublicUrl is not configured")).TrimEnd('/');
    }

    public async Task<CheckoutSessionResult> CreateSessionAsync(Guid orderId, Guid userId)
    {
        var order = await _orders.GetByIdAsync(orderId);
        if (order is null || order.UserId != userId) return new(false, null, "NotFound");
        if (order.Status != OrderStatuses.Pending || order.PaymentMethod == PaymentMethodNames.CashOnDelivery)
            return new(false, null, "NotPayable");

        var items = await _orders.GetItemsAsync(orderId);
        var lines = items.Select(i => Line(i.ProductName, i.UnitPrice, i.Quantity)).ToList();
        if (order.Tax > 0) lines.Add(Line("VAT", order.Tax, 1));
        if (order.ShippingCost > 0) lines.Add(Line("Shipping", order.ShippingCost, 1));

        var sumCents = lines.Sum(l => (l.PriceData!.UnitAmount ?? 0) * (l.Quantity ?? 1));
        if (sumCents != Cents(order.Total))
            throw new InvalidOperationException($"Order {order.OrderNumber}: Stripe lines {sumCents} != order total {Cents(order.Total)}");

        var contact = await _users.GetContactAsync(userId);
        var meta = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = order.Id.ToString(),
            CustomerEmail = contact?.Email,
            Metadata = meta,
            PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = meta },
            LineItems = lines,
            SuccessUrl = $"{_publicUrl}/orders/{order.Id}/confirmation?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_publicUrl}/orders/{order.Id}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(31) // Stripe minimum is 30
        };

        var session = await new SessionService().CreateAsync(options, new RequestOptions
        {
            // a double click within the same minute reuses the same session; a later retry gets a fresh one
            IdempotencyKey = $"checkout-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmm}"
        });
        return new(true, session.Url, null);
    }

    private static long Cents(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static SessionLineItemOptions Line(string name, decimal unitPrice, int qty) => new()
    {
        Quantity = qty,
        PriceData = new SessionLineItemPriceDataOptions
        {
            Currency = "eur",
            UnitAmount = Cents(unitPrice),
            ProductData = new SessionLineItemPriceDataProductDataOptions { Name = name }
        }
    };
}
