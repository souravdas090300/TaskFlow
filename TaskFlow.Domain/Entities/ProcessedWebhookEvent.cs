using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// Tracks processed Stripe webhook events to ensure idempotency.
/// EventId is the primary key with a unique constraint in the database.
/// This prevents duplicate processing of the same webhook event.
/// </summary>
public class ProcessedWebhookEvent
{
    [Key]
    [MaxLength(255)]
    public string EventId { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
