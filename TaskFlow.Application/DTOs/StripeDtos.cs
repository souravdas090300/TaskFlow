namespace TaskFlow.Application.DTOs;

public record CreateCheckoutRequest(Guid OrderId);

public class StripeWebhookDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public StripeEventData? Data { get; set; }
}

public class StripeEventData
{
    public StripeEventObject? Object { get; set; }
}

public class StripeEventObject
{
    public string Id { get; set; } = string.Empty;
    public string? PaymentIntentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ClientSecret { get; set; }
    public string? Metadata { get; set; }
}
