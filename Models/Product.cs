using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; } = 0;

        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        // REMOVED: StockQuantity is now managed strictly via the Inventory model.

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? Color { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        public DateTime? PublishAt { get; set; } = DateTime.UtcNow;

        [Required]
        public long CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Categories? Category { get; set; }

        [Required]
        public long SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public virtual Auth? Seller { get; set; }

        // --- Navigation Properties ---
        public virtual ICollection<ProductVariants> Variants { get; set; } = new List<ProductVariants>();

        // Added: Direct link to inventory if the product has no variants
        public virtual Inventory? Inventory { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}