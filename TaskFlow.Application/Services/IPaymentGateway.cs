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
}
