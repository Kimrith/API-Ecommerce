using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using API_Ecommerce.Enums;

namespace API_Ecommerce.DTOs
{
    // --- Request for Creating a Category ---
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public IFormFile? Image { get; set; }

        // Optional: Sellers can save as Draft or submit as Pending
        public CategoriesStatus Status { get; set; } = CategoriesStatus.Pending;
    }

    // --- Request for Updating Category Info ---
    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public IFormFile? Image { get; set; }

        public CategoriesStatus Status { get; set; }
    }

    // --- Request for Admin Approval/Rejection ---
    public class UpdateCategoryStatusDto
    {
        [Required]
        public CategoriesStatus Status { get; set; }
    }

    // --- Response sent back to Client ---
    public class CategoryResponseDto
    {
        public long Id { get; set; } // Updated from int to long
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public CategoriesStatus Status { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int ProductCount { get; set; }

        // --- Creator Info ---
        public long UserId { get; set; } // Updated from int to long
        public string CreatedBy { get; set; } = string.Empty; // FullName or Email of creator

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}