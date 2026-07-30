using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    public class Product
    {
        [Key]
        public long Id { get; set; } // Updated from int to long

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // --- Discount Fields ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; } = 0;

        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        public int StockQuantity { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        // --- Scheduled Posting ---
        public DateTime? PublishAt { get; set; } = DateTime.UtcNow;

        // --- Category Relationship ---
        [Required]
        public long CategoryId { get; set; } // Updated from int to long (matches Categories.Id)

        [ForeignKey(nameof(CategoryId))]
        public virtual Categories? Category { get; set; }

        // --- Seller / Creator Relationship ---
        [Required]
        public long SellerId { get; set; } // Updated from int to long (matches Auth.Id)

        [ForeignKey(nameof(SellerId))]
        public virtual Auth? Seller { get; set; }

        // --- Navigation to Variants ---
        public virtual ICollection<ProductVariants> Variants { get; set; } = new List<ProductVariants>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}