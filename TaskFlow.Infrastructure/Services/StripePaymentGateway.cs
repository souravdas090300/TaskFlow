using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using TaskFlow.Application.Services;

namespace TaskFlow.Infrastructure.Services;

/// <summary>
/// Stripe payment gateway implementation using official Stripe.net SDK.
/// </summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly string _webhookSecret;

    public StripePaymentGateway(IConfiguration configuration)
    {
        var secretKey = configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey must be configured");
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? throw new InvalidOperationException("Stripe:WebhookSecret must be configured");
        
        StripeConfiguration.ApiKey = secretKey;
    }

    public bool VerifyWebhookSignature(string jsonPayload, string sigHeader)
    {
        if (string.IsNullOrEmpty(sigHeader))
        {
            Console.WriteLine("Stripe-Signature header missing");
            return false;
        }

        try
        {
            EventUtility.ConstructEvent(
                jsonPayload,
                sigHeader,
                _webhookSecret,
                300 // tolerance in seconds
            );

            return true;
        }
        catch (StripeException e)
        {
            Console.WriteLine($"Webhook signature verification failed: {e.Message}");
            return false;
        }
    }

    public (string? PaymentIntentId, Guid? OrderId) GetPaymentDetailsFromEvent(string jsonPayload, string sigHeader)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                jsonPayload,
                sigHeader,
                _webhookSecret,
                300
            );

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                var orderIdStr = GetMeta(paymentIntent?.Metadata, "orderId") ?? GetMeta(paymentIntent?.Metadata, "OrderId");

                if (Guid.TryParse(orderIdStr, out var orderId))
                {
                    return (paymentIntent?.Id, orderId);
                }
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                // Only act on fully paid sessions; unpaid/async sessions are ignored.
                if (session?.PaymentStatus != "paid")
                {
                    return (null, null);
                }

                var orderIdStr = GetMeta(session.Metadata, "orderId") ?? session.ClientReferenceId;
                if (Guid.TryParse(orderIdStr, out var orderId))
                {
                    return (session.PaymentIntent?.Id, orderId);
                }
            }

            return (null, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to parse webhook event: {e.Message}");
            return (null, null);
        }
    }

    public async Task<(bool Ok, string? RefundId, string? Error)> RefundPaymentAsync(string paymentIntentId)
    {
        try
        {
            var refund = await new RefundService().CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            });

            return refund.Status is "succeeded" or "pending"
                ? (true, refund.Id, null)
                : (false, null, $"Refund status: {refund.Status}");
        }
        catch (StripeException e)
        {
            return (false, null, e.Message);
        }
    }

    private static string? GetMeta(IDictionary<string, string?>? metadata, string key)
        => metadata != null && metadata.TryGetValue(key, out var value) ? value : null;
}
