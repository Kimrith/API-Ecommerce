using System.ComponentModel.DataAnnotations;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    public class Auth
    {
        [Key]
        public long Id { get; set; } // Changed to long to match BIGINT foreign keys

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // --- New Seller Fields ---
        public string? ShopName { get; set; }

        public AuthStatus Status { get; set; } = AuthStatus.Active;

        public string? ProfileImageUrl { get; set; }

        public Roles Role { get; set; } = Roles.Seller;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // --- Navigation Property for Addresses ---
        public virtual ICollection<Address  > Addresses { get; set; } = new List<Address>();
    }
}