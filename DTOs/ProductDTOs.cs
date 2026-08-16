using System.ComponentModel.DataAnnotations;
using API_Ecommerce.Enums;
using Microsoft.AspNetCore.Http;

namespace API_Ecommerce.DTOs
{
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ProductResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        public decimal EffectivePrice =>
            (DiscountPrice.HasValue && DiscountPrice.Value > 0 &&
             (!DiscountStartDate.HasValue || DiscountStartDate <= DateTime.UtcNow) &&
             (!DiscountEndDate.HasValue || DiscountEndDate >= DateTime.UtcNow))
            ? DiscountPrice.Value
            : Price;

        // --- Stock Info (Pulled from Inventory relationship) ---
        public int StockQuantity { get; set; } // Can map from Inventory.Quantity if no variants
        public int AvailableQuantity { get; set; } // Inventory.AvailableQuantity

        public string? Size { get; set; }
        public string? Color { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public ProductStatus Status { get; set; }
        public DateTime? PublishAt { get; set; }

        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public long SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string SellerRole { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Optional nested variants if fetching complete details
        public List<ProductVariantResponseDto> Variants { get; set; } = new();
    }

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

        [Range(0, double.MaxValue, ErrorMessage = "Discount price cannot be negative.")]
        public decimal? DiscountPrice { get; set; }

        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        // --- Initial Stock (Used to seed the Inventory table if no variants) ---
        [Range(0, int.MaxValue, ErrorMessage = "Initial stock cannot be negative.")]
        public int InitialStock { get; set; } = 0;

        [Required(ErrorMessage = "Category ID is required.")]
        public long CategoryId { get; set; }

        [StringLength(100, ErrorMessage = "Size cannot exceed 100 characters.")]
        public string? Size { get; set; }

        [StringLength(100, ErrorMessage = "Color cannot exceed 100 characters.")]
        public string? Color { get; set; }

        public DateTime? PublishAt { get; set; }
        public IFormFile? Image { get; set; }

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

        [Range(0, double.MaxValue, ErrorMessage = "Discount price cannot be negative.")]
        public decimal? DiscountPrice { get; set; }

        public DateTime? DiscountStartDate { get; set; }
        public DateTime? DiscountEndDate { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        public long CategoryId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Initial stock cannot be negative.")]
        public int InitialStock { get; set; } = 0;

        public ProductStatus Status { get; set; }
        [StringLength(100, ErrorMessage = "Size cannot exceed 100 characters.")]
        public string? Size { get; set; }

        [StringLength(100, ErrorMessage = "Color cannot exceed 100 characters.")]
        public string? Color { get; set; }

        public DateTime? PublishAt { get; set; }
        public IFormFile? Image { get; set; }

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

    public class UpdateProductStatusDto
    {
        [Required(ErrorMessage = "Status is required.")]
        public ProductStatus Status { get; set; }
    }

    public class TopSellingProductDto
    {
        public long ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Slug { get; set; }
        public decimal Price { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }
}