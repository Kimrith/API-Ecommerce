using System.ComponentModel.DataAnnotations;
using API_Ecommerce.Enums;
using Microsoft.AspNetCore.Http;

namespace API_Ecommerce.DTOs
{
    // --- 1. Response DTO ---
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public ProductStatus Status { get; set; }

        // Category details
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        // Seller details
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // --- 2. Create Product DTO ---
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150, ErrorMessage = "Product name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        public int CategoryId { get; set; }

        // Optional image upload via Form File
        public IFormFile? Image { get; set; }
    }

    // --- 3. Update Product DTO ---
    public class UpdateProductDto
    {
        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150, ErrorMessage = "Product name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category ID is required.")]
        public int CategoryId { get; set; }

        public ProductStatus Status { get; set; }

        // Optional new image upload (replaces existing image if provided)
        public IFormFile? Image { get; set; }
    }

    // --- 4. Update Product Status DTO ---
    public class UpdateProductStatusDto
    {
        [Required(ErrorMessage = "Status is required.")]
        public ProductStatus Status { get; set; }
    }
}