namespace TaskFlow.Application.Services;

/// <summary>
/// Payment gateway interface for webhook verification and event parsing.
/// Checkout sessions are created by StripeCheckoutService in Infrastructure.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Verifies the webhook signature from the payment provider.
    /// </summary>
    bool VerifyWebhookSignature(string jsonPayload, string sigHeader);

    /// <summary>
    /// Extracts payment details from a webhook event.
    /// </summary>
    (string? PaymentIntentId, Guid? OrderId) GetPaymentDetailsFromEvent(string jsonPayload, string sigHeader);

    /// <summary>
    /// Refunds a Stripe payment intent in full. Returns the refund id on success,
    /// or an error message on failure.
    /// </summary>
    Task<(bool Ok, string? RefundId, string? Error)> RefundPaymentAsync(string paymentIntentId);
}
