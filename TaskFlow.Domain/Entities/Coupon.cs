using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Domain.Entities
{
    public class Coupon
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        // Discount type: Percentage or FixedAmount
        [Required]
        [MaxLength(50)]
        public string DiscountType { get; set; } = "Percentage"; // Percentage, FixedAmount

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; } = 0;

        // Minimum order amount to use coupon
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumOrderAmount { get; set; } = 0;

        // Maximum discount amount (for percentage)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscountAmount { get; set; }

        // Usage limits
        public int UsageLimit { get; set; } = 0; // 0 = unlimited

        public int TimesUsed { get; set; } = 0;

        // Per user limit
        public int UsageLimitPerUser { get; set; } = 1;

        // Validity period
        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidUntil { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
