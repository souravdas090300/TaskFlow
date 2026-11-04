using Stripe;
using TaskFlow.Application.DTOs;

namespace TaskFlow.API.Services;

public class StripeService
{
    private readonly string _webhookSecret;
    private readonly string _secretKey;

    public StripeService(IConfiguration configuration)
    {
        _secretKey = configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey must be configured");
        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? throw new InvalidOperationException("Stripe:WebhookSecret must be configured");
        
        StripeConfiguration.ApiKey = _secretKey;
    }

    public async Task<CheckoutSessionResponse?> CreateCheckoutSessionAsync(Guid orderId, decimal amount, string successUrl, string cancelUrl, string? orderNumber = null)
    {
        // TODO: Implement with proper Stripe.net checkout session creation
        // For now, return mock to allow build
        Console.WriteLine($"Stripe checkout session requested for order {orderId} with amount {amount}");

        return new CheckoutSessionResponse
        {
            SessionId = Guid.NewGuid().ToString(),
            CheckoutUrl = successUrl
        };
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
            var stripeEvent = EventUtility.ConstructEvent(
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
                var orderIdStr = paymentIntent?.Metadata["OrderId"];
                
                if (Guid.TryParse(orderIdStr, out var orderId))
                {
                    return (paymentIntent?.Id, orderId);
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
}
