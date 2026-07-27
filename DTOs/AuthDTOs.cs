using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // Added for JsonStringEnumConverter
using API_Ecommerce.Enums;
using Microsoft.AspNetCore.Http;

namespace API_Ecommerce.DTOs
{
    // --- 1. Unified Request for User / Seller / Admin Registration ---
    public class RegisterDto
    {
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Role Selection (Customer, Seller, Admin)
        // Decorator forces OpenAPI and Model Binding to handle Enum as string
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Roles Role { get; set; } = Roles.Customer;

        // Optional fields for Sellers/Users
        public string? ShopName { get; set; }

        public string? Address { get; set; }

        public IFormFile? ProfileImage { get; set; }
    }

    // --- 2. Request for Updating Existing User Profile ---
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Optional: Leave null/empty if password is not changing
        public string? Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        public string? ShopName { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AuthStatus Status { get; set; } = AuthStatus.Active;

        public string? Address { get; set; }

        public IFormFile? ProfileImage { get; set; }
    }

    // --- 3. Request for User Login ---
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    // --- 4. Response sent back after successful Auth Operations ---
    public class AuthResponseDto
    {
        public long UserId { get; set; } // Updated from int to long
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        public string? ShopName { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AuthStatus? Status { get; set; }

        public string? ProfileImageUrl { get; set; }

        // Returning user addresses as structured DTOs rather than a single string
        public List<AddressResponseDto> Addresses { get; set; } = new();

        public long? SellerId => Role == "Seller" ? UserId : null;
    }
}