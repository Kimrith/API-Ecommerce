using System.ComponentModel.DataAnnotations;

namespace API_Ecommerce.DTOs
{
    public class FavoriteResponseDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ProductId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Product Details matching your ProductResponseDto style
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string ProductImageUrl { get; set; } = string.Empty;
    }

    public class CreateFavoriteDto
    {
        [Required(ErrorMessage = "Product ID is required.")]
        public long ProductId { get; set; }
    }
}