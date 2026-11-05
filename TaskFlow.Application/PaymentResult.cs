namespace TaskFlow.Application;

/// <summary>
/// Result of processing a Stripe payment webhook event.
/// </summary>
public enum PaymentResult
{
    /// <summary>
    /// Payment was successfully processed. Order is Paid, stock decremented, email sent.
    /// </summary>
    Processed,

    /// <summary>
    /// Event was already processed (idempotent). No changes made.
    /// </summary>
    AlreadyProcessed,

    /// <summary>
    /// Order not found. Customer was charged but order does not exist.
    /// </summary>
    OrderNotFound,

    /// <summary>
    /// Payment rejected. Order is in a terminal state (Cancelled, Shipped, Delivered).
    /// </summary>
    Rejected,

    /// <summary>
    /// Stock conflict. Customer paid but stock is insufficient. Order flagged PaymentReview.
    /// </summary>
    StockConflict
}
