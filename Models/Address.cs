using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    public class Address
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long UserId { get; set; }

        // Navigation property pointing back to the user/auth entity
        [ForeignKey(nameof(UserId))]
        public virtual Auth User { get; set; } = null!;

        [Required]
        public AddressType AddressType { get; set; } // 'shipping' or 'billing'

        [Required]
        [MaxLength(255)]
        public string StreetAddress { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? State { get; set; }

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}