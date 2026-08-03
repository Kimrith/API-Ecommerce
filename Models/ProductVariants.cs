using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("product_variants")]
    public class ProductVariants
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Sku { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; } = 0;

        // REMOVED: StockQuantity managed via Inventory table.

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? Color { get; set; }

        public bool IsActive { get; set; } = true;

        // --- Navigation Properties ---
        // Added: Link to inventory specifically for this variant
        public virtual Inventory? Inventory { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}