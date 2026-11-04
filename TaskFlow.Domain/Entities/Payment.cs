using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Domain.Entities
{
    public class Payment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        // Payment method: Stripe, CashOnDelivery
        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        // Stripe payment ID
        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        // Payment status: Pending, Completed, Failed, Refunded
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0;

        [MaxLength(3)]
        public string? Currency { get; set; } = "USD";

        // Stripe webhook event ID (for idempotency)
        [MaxLength(255)]
        public string? StripeEventId { get; set; }

        // Refund information
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }

        [MaxLength(255)]
        public string? RefundId { get; set; }

        public DateTime? RefundedAt { get; set; }

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;
    }
}
