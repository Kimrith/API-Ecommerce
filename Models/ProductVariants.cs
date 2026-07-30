using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    // Same Product
    public class ProductVariants
    {
        [Key]
        public long Id { get; set; }

        // --- Foreign Key to Parent Product ---
        [Required]
        public long ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        // --- Variant Attributes ---
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty; // e.g., "Red / 330ml" or "Large"

        [StringLength(100)]
        public string? Sku { get; set; } // Stock Keeping Unit (e.g., "COKE-330-CAN")

        // --- Pricing & Stock Overrides ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Specific price for this variant

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; } = 0;

        public int StockQuantity { get; set; } = 0;

        [StringLength(500)]
        public string? ImageUrl { get; set; } // Variant-specific image (e.g., Red shirt vs Blue shirt)

        // --- Variant Specific Attributes (Optional JSON or String) ---
        [StringLength(100)]
        public string? Size { get; set; } // e.g., "330ml", "XL"

        [StringLength(100)]
        public string? Color { get; set; } // e.g., "Red", "Zero Sugar"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}