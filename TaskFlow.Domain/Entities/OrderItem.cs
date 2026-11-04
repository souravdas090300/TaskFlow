using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Domain.Entities
{
    public class OrderItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        // If using product variants
        public Guid? ProductVariantId { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        // Price snapshot at time of order (immutable)
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Size and color snapshot (if using variants)
        [MaxLength(100)]
        public string? Size { get; set; }

        [MaxLength(100)]
        public string? Color { get; set; }

        // Product name snapshot (in case product is deleted)
        [Required]
        [MaxLength(255)]
        public string ProductName { get; set; } = string.Empty;

        // Product image snapshot
        [MaxLength(500)]
        public string? ProductImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [ForeignKey(nameof(ProductVariantId))]
        public ProductVariant? ProductVariant { get; set; }
    }
}
