using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("order_items")]
    public class OrderItem
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        [Required]
        public long ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        public long? VariantId { get; set; }

        [ForeignKey(nameof(VariantId))]
        public virtual ProductVariants? Variant { get; set; }

        // --- Historical Snapshot Fields ---
        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? VariantName { get; set; }

        [StringLength(50)]
        public string? Sku { get; set; }

        // --- Price & Quantity ---
        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}