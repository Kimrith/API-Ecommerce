using System.ComponentModel.DataAnnotations;

namespace API_Ecommerce.DTOs
{
    public class CartDtos
    {
        // Response DTO returned to client
        public class Response
        {
            public long Id { get; set; }
            public long? UserId { get; set; }
            public string? SessionId { get; set; }
            public List<CartItemDtos.Response> Items { get; set; } = new();
            public decimal TotalAmount { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        // Request DTO to merge guest cart into user cart upon login
        public class Merge
        {
            [Required]
            public string GuestSessionId { get; set; } = string.Empty;
        }
    }
}