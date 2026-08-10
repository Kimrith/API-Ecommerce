using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace API_Ecommerce.DTOs
{
    // --- 1. Response DTO ---
    public class BannerResponseDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? ImageUrl { get; set; } = string.Empty;
        public string? TargetUrl { get; set; }
        public string Position { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // --- 2. Create Banner DTO ---
    public class CreateBannerDto : IValidatableObject
    {
        [Required(ErrorMessage = "Banner title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Subtitle cannot exceed 255 characters.")]
        public string? Subtitle { get; set; }

        [StringLength(500, ErrorMessage = "Target URL cannot exceed 500 characters.")]
        public string? TargetUrl { get; set; }

        [StringLength(50, ErrorMessage = "Position cannot exceed 50 characters.")]
        public string Position { get; set; } = "MainHome";

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        [Required(ErrorMessage = "Banner image is required.")]
        public IFormFile? Image { get; set; } = null!;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartsAt.HasValue && ExpiresAt.HasValue && ExpiresAt < StartsAt)
            {
                yield return new ValidationResult(
                    "Expiration date cannot be earlier than the start date.",
                    new[] { nameof(ExpiresAt) });
            }
        }
    }

    // --- 3. Update Banner DTO ---
    public class UpdateBannerDto : IValidatableObject
    {
        [Required(ErrorMessage = "Banner title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Subtitle cannot exceed 255 characters.")]
        public string? Subtitle { get; set; }

        [StringLength(500, ErrorMessage = "Target URL cannot exceed 500 characters.")]
        public string? TargetUrl { get; set; }

        [StringLength(50, ErrorMessage = "Position cannot exceed 50 characters.")]
        public string Position { get; set; } = "MainHome";

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        // Optional: Leave null if keeping the existing image
        public IFormFile? Image { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartsAt.HasValue && ExpiresAt.HasValue && ExpiresAt < StartsAt)
            {
                yield return new ValidationResult(
                    "Expiration date cannot be earlier than the start date.",
                    new[] { nameof(ExpiresAt) });
            }
        }
    }

    // --- 4. Update Status DTO ---
    public class UpdateBannerStatusDto
    {
        public bool IsActive { get; set; }
    }
}