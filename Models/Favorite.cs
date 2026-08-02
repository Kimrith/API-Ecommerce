using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // Required for [Index]

namespace API_Ecommerce.Models
{
    [Table("favorites")]
    [Index(nameof(UserId), nameof(ProductId), IsUnique = true)] // Prevents duplicate favorites
    public class Favorite
    {
        [Key]
        public long Id { get; set; }

        // --- Customer Relationship ---
        [Required]
        public long UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual Auth? User { get; set; }

        // --- Product Relationship ---
        [Required]
        public long ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}