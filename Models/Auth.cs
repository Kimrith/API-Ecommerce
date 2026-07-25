using System.ComponentModel.DataAnnotations;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    public class Auth
    {
        [Key]
        public int Id { get; set; }

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

        // Using Status enum instead of string
        public AuthStatus Status { get; set; } = AuthStatus.Active;

        public string? Address { get; set; }

        public string? ProfileImageUrl { get; set; }

        // Set default to Seller or Customer as needed
        public Roles Role { get; set; } = Roles.Seller;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}