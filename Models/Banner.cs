using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("banners")]
    public class Banner
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty; // e.g., "Grand Opening Sale - Up to 50% Off"

        [StringLength(255)]
        public string? Subtitle { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty; // High-res banner image URL

        // Optional link destination when a user clicks the banner (e.g., "/products/12" or "/categories/electronics")
        [StringLength(500)]
        public string? TargetUrl { get; set; }

        // Placement area on frontend: e.g., "MainHome", "CategoryHeader", "Sidebar"
        [StringLength(50)]
        public string Position { get; set; } = "MainHome";

        // Controls display priority (0 = first, 1 = second, etc.)
        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Optional schedule window for seasonal promos
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}