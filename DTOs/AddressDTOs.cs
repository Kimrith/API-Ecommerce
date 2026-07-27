using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API_Ecommerce.Enums;

namespace API_Ecommerce.DTOs
{
    // --- 1. DTO for Creating a New Address ---
    public class CreateAddressDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AddressType AddressType { get; set; } = AddressType.Shipping;

        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(255, ErrorMessage = "Street address cannot exceed 255 characters.")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string City { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
        public string? State { get; set; }

        [Required(ErrorMessage = "Postal code is required.")]
        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters.")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        public string Country { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;
    }

    // --- 2. DTO for Updating an Existing Address ---
    public class UpdateAddressDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AddressType AddressType { get; set; }

        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(255, ErrorMessage = "Street address cannot exceed 255 characters.")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string City { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
        public string? State { get; set; }

        [Required(ErrorMessage = "Postal code is required.")]
        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters.")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        public string Country { get; set; } = string.Empty;

        public bool IsDefault { get; set; }
    }

    // --- 3. DTO for Address Responses ---
    public class AddressResponseDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AddressType AddressType { get; set; }

        public string StreetAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}