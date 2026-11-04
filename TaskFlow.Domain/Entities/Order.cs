using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Domain.Entities
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        // Order number (human-readable)
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        // Status flow: Pending -> Paid -> Processing -> Shipped -> Delivered -> Cancelled
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; } = 0;

        // Shipping method
        [MaxLength(100)]
        public string? ShippingMethod { get; set; }

        // Payment method: Stripe, CashOnDelivery
        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        // Addresses
        public Guid? ShippingAddressId { get; set; }

        public Guid? BillingAddressId { get; set; }

        // Customer notes
        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Tracking number
        [MaxLength(255)]
        public string? TrackingNumber { get; set; }

        // Cancellation
        public DateTime? CancelledAt { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(ShippingAddressId))]
        public Address? ShippingAddress { get; set; }

        [ForeignKey(nameof(BillingAddressId))]
        public Address? BillingAddress { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public Payment? Payment { get; set; }
        public Coupon? AppliedCoupon { get; set; }
    }
}
