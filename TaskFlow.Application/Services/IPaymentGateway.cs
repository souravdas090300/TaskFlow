using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Services;

/// <summary>
/// Payment gateway interface for processing payments.
/// Implementations can use Stripe, PayPal, or other payment providers.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Creates a checkout session for the given order.
    /// </summary>
    Task<CheckoutSessionResponse?> CreateCheckoutSessionAsync(Guid orderId, decimal amount, string successUrl, string cancelUrl, string? orderNumber = null);

    /// <summary>
    /// Verifies the webhook signature from the payment provider.
    /// </summary>
    bool VerifyWebhookSignature(string jsonPayload, string sigHeader);

    /// <summary>
    /// Extracts payment details from a webhook event.
    /// </summary>
    (string? PaymentIntentId, Guid? OrderId) GetPaymentDetailsFromEvent(string jsonPayload, string sigHeader);
}
