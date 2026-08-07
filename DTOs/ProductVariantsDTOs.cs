using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace API_Ecommerce.DTOs
{
    public class CreateProductVariantDto
    {
        [Required(ErrorMessage = "Parent product ID is required.")]
        public long ProductId { get; set; }

        [Required(ErrorMessage = "Variant title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "SKU cannot exceed 100 characters.")]
        public string? Sku { get; set; }

        // CORRECT: IFormFile is correct here for receiving uploaded files
        public IFormFile? ImageUrl { get; set; }

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? Color { get; set; }

        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public int InitialStock { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateProductVariantDto
    {
        [Required(ErrorMessage = "Variant title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "SKU cannot exceed 100 characters.")]
        public string? Sku { get; set; }

        // CORRECT: IFormFile is correct here for receiving uploaded files on update
        public IFormFile? ImageUrl { get; set; }

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? Color { get; set; }

        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public int InitialStock { get; set; }

        public bool IsActive { get; set; }
    }

    public class ProductVariantResponseDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public int AvailableQuantity { get; set; }

        // CORRECT: Returns the image URL string back to the API consumer
        public string? ImageUrl { get; set; }

        public string? Size { get; set; }
        public string? Color { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int InitialStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}