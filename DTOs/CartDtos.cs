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

            // --- Financial Breakdown ---
            public decimal SubtotalAmount { get; set; }       // Sum of all items before discount
            public string? AppliedCouponCode { get; set; }    // Code if a coupon is active
            public decimal DiscountAmount { get; set; }       // Amount saved from the coupon
            public decimal TotalAmount { get; set; }          // Final amount to pay (Subtotal - Discount)
            // ---------------------------

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

        // Request DTO to apply a coupon to the cart
        public class ApplyCoupon
        {
            [Required]
            [StringLength(50)]
            public string Code { get; set; } = string.Empty;
        }
    }
}