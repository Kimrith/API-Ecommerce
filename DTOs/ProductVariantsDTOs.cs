using System.ComponentModel.DataAnnotations;

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

        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? Color { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateProductVariantDto
    {
        [Required(ErrorMessage = "Variant title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "SKU cannot exceed 100 characters.")]
        public string? Sku { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? Size { get; set; }

        [StringLength(100)]
        public string? Color { get; set; }

        public bool IsActive { get; set; }
    }

    public class ProductVariantResponseDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public int AvailableQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}