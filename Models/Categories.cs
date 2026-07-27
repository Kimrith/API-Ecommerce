using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    public class Categories
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string Slug { get; set; } = string.Empty;

        public CategoriesStatus Status { get; set; } = CategoriesStatus.Pending;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        // --- Track Creator (Seller / Admin) ---
        [Required]
        public long UserId { get; set; } // Updated from int to long

        [ForeignKey(nameof(UserId))]
        public virtual Auth? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // --- Navigation Property to Products ---
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}