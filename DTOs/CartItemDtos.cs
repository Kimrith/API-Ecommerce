using System.ComponentModel.DataAnnotations;

namespace API_Ecommerce.DTOs
{
    public class CartItemDtos
    {
        // Response DTO for individual items
        public class Response
        {
            public long Id { get; set; }
            public long ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string? ProductImageUrl { get; set; }

            public long? VariantId { get; set; }
            public string? VariantName { get; set; }

            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Subtotal => Quantity * Price;
        }

        // Request DTO for adding an item
        public class Create
        {
            [Required]
            public long ProductId { get; set; }

            public long? VariantId { get; set; }

            [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99.")]
            public int Quantity { get; set; } = 1;
        }

        // Request DTO for updating quantity
        public class UpdateQuantity
        {
            [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99.")]
            public int Quantity { get; set; }
        }
    }
}