using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        public long Id { get; set; }

        // --- Recipient User ---
        [Required]
        public long UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual Auth? User { get; set; }

        // --- Notification Content ---
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string Type { get; set; } = "OrderUpdate"; // e.g., OrderUpdate, Payment, Promo

        // Optional deep-link URL (e.g., "/orders/12")
        [StringLength(255)]
        public string? TargetUrl { get; set; }

        // --- Read Status ---
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}