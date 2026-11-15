namespace TaskFlow.Domain.Entities;

public enum PaymentIssueType
{
    OrderNotFound,   // Stripe event for an order we cannot find
    OrderClosed,     // payment arrived for a Cancelled/Shipped/Delivered order
    DoubleCharge,    // order already paid with a different payment
    StockConflict    // paid, but stock ran out (order is in PaymentReview)
}

/// <summary>A payment that needs a human (usually a refund). One row per Stripe event.</summary>
public class PaymentIssue
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public PaymentIssueType Type { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }

    public static PaymentIssue Create(Guid? orderId, string? orderNumber, string paymentIntentId, string eventId,
        PaymentIssueType type, string details, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(), OrderId = orderId, OrderNumber = orderNumber, PaymentIntentId = paymentIntentId,
        EventId = eventId, Type = type, Details = details, CreatedAt = nowUtc
    };
}
