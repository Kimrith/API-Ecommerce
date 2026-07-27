using System.ComponentModel.DataAnnotations;
using API_Ecommerce.Enums;
using Microsoft.AspNetCore.Http;

namespace API_Ecommerce.DTOs
{
    // --- Generic Pagination Response ---
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // --- 1. Response DTO ---
    public class ProductResponseDto
    {
        public long Id { get; set; } // Updated from int to long
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }

        public decimal Price { get; set; }

        // --- Discount Properties (Nullable to match Entity) ---
        public decimal? DiscountPrice { get; set; }
        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        // --- Computed Effective Price ---
        // Returns DiscountPrice if active today, otherwise standard Price
        public decimal EffectivePrice =>
            (DiscountPrice.HasValue && DiscountPrice.Value > 0 &&
             (!DiscountStartDate.HasValue || DiscountStartDate <= DateTime.UtcNow) &&
             (!DiscountEndDate.HasValue || DiscountEndDate >= DateTime.UtcNow))
            ? DiscountPrice.Value
            : Price;

        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public ProductStatus Status { get; set; }

        // --- Scheduled Posting ---
        public DateTime? PublishAt { get; set; }

        // Category details
        public long CategoryId { get; set; } // Updated from int to long
        public string CategoryName { get; set; } = string.Empty;

        // Seller details
        public long SellerId { get; set; } // Updated from int to long
        public string SellerName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // --- 2. Create Product DTO ---
    public class CreateProductDto : IValidatableObject
    {
        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150, ErrorMessage = "Product name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        // --- Discount Inputs ---
        [Range(0, double.MaxValue, ErrorMessage = "Discount price cannot be negative.")]
        public decimal? DiscountPrice { get; set; }

        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        public long CategoryId { get; set; } // Updated from int to long

        // --- Scheduled Post Date ---
        // Leave null to publish immediately (UTC Now)
        public DateTime? PublishAt { get; set; }

        // Optional image upload via Form File
        public IFormFile? Image { get; set; }

        // Business Logic Validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DiscountPrice.HasValue && DiscountPrice.Value >= Price && DiscountPrice.Value > 0)
            {
                yield return new ValidationResult(
                    "Discount price must be less than the regular price.",
                    new[] { nameof(DiscountPrice) });
            }

            if (DiscountStartDate.HasValue && DiscountEndDate.HasValue && DiscountEndDate < DiscountStartDate)
            {
                yield return new ValidationResult(
                    "Discount end date cannot be earlier than start date.",
                    new[] { nameof(DiscountEndDate) });
            }
        }
    }

    // --- 3. Update Product DTO ---
    public class UpdateProductDto : IValidatableObject
    {
        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150, ErrorMessage = "Product name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        // --- Discount Inputs ---
        [Range(0, double.MaxValue, ErrorMessage = "Discount price cannot be negative.")]
        public decimal? DiscountPrice { get; set; }

        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        public long CategoryId { get; set; } // Updated from int to long

        public ProductStatus Status { get; set; }

        // --- Scheduled Post Date ---
        public DateTime? PublishAt { get; set; }

        // Optional new image upload (replaces existing image if provided)
        public IFormFile? Image { get; set; }

        // Business Logic Validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DiscountPrice.HasValue && DiscountPrice.Value >= Price && DiscountPrice.Value > 0)
            {
                yield return new ValidationResult(
                    "Discount price must be less than the regular price.",
                    new[] { nameof(DiscountPrice) });
            }

            if (DiscountStartDate.HasValue && DiscountEndDate.HasValue && DiscountEndDate < DiscountStartDate)
            {
                yield return new ValidationResult(
                    "Discount end date cannot be earlier than start date.",
                    new[] { nameof(DiscountEndDate) });
            }
        }
    }

    // --- 4. Update Product Status DTO ---
    public class UpdateProductStatusDto
    {
        [Required(ErrorMessage = "Status is required.")]
        public ProductStatus Status { get; set; }
    }
}