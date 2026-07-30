using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("reviews")]
    public class Review
    {
        [Key]
        public long Id { get; set; }

        // --- Product Relationship ---
        [Required]
        public long ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        // --- Customer Relationship ---
        [Required]
        public long UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual Auth? User { get; set; }

        // --- Verified Purchase Link (Optional) ---
        // Links to the exact order item to ensure the user actually bought the item
        public long? OrderItemId { get; set; }

        [ForeignKey(nameof(OrderItemId))]
        public virtual OrderItem? OrderItem { get; set; }

        // --- Rating & Content ---
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; } // 1 to 5 stars

        [StringLength(200)]
        public string? Title { get; set; } // Short summary (e.g., "Great quality!")

        [StringLength(2000)]
        public string? Comment { get; set; } // Detailed feedback

        // --- Moderation & Verification Flags ---
        public bool IsVerifiedPurchase { get; set; } = false;

        public bool IsApproved { get; set; } = true; // Set to false if you require admin approval before publishing

        // --- Timestamps ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}