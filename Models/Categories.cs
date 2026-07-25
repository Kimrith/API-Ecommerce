using System.ComponentModel.DataAnnotations;

namespace API_Ecommerce.Models
{
    public class Categories
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string Slug { get; set; } = string.Empty; // e.g., "mens-clothing"

        [StringLength(500)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // --- Navigation Property to Products ---
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}