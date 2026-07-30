using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        // --- Admin/User Information ---
        public long? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual Auth? User { get; set; }

        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // e.g., "CREATE", "UPDATE", "DELETE", "LOGIN_FAILED"

        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty; // e.g., "Product", "Order", "Coupon"

        [StringLength(100)]
        public string? EntityId { get; set; } // Primary key ID of the affected item (e.g., "1024")

        // --- Change Data (JSON format) ---
        public string? OldValues { get; set; } // JSON snapshot of state BEFORE change
        public string? NewValues { get; set; } // JSON snapshot of state AFTER change

        // --- Network & Client Info ---
        [StringLength(45)]
        public string? IpAddress { get; set; } // IPv4 or IPv6 address

        [StringLength(500)]
        public string? UserAgent { get; set; } // Browser / Client details

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}