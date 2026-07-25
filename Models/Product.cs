using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int StockQuantity { get; set; } = 0;

        public string ImageUrl { get; set; } = string.Empty;

        // Status for Admin moderation
        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        // --- Foreign Key to Seller (Auth) ---
        [Required]
        public int SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public Auth Seller { get; set; } = null!;

        // --- Foreign Key to Category ---
        [Required]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Categories Category { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}