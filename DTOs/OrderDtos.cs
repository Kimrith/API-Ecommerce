using API_Ecommerce.Commands.Cart;
using API_Ecommerce.Enums;

namespace API_Ecommerce.DTOs
{
    public class OrderDtos
    {
        // ==========================================
        // 1. CREATE ORDER REQUEST (Checkout Payload)
        // ==========================================
        public record Create(
            long ShippingAddressId,
            long? BillingAddressId, // Optional: defaults to ShippingAddressId if null in handler
            string? Notes,
            string? CouponCode
        );

        // ==========================================
        // 2. FULL CHECKOUT REQUEST (From Controller / Frontend)
        // ==========================================
        public record CheckoutRequest(
            Create OrderDetails,
            List<CartItemDto> CartItems,
            string? Currency
        );

        // ==========================================
        // 3. UPDATE STATUS REQUEST (Admin / Payment Callback)
        // ==========================================
        public record UpdateStatus(
            OrderStatus Status
        );

        // ==========================================
        // 4. FULL ORDER RESPONSE
        // ==========================================
        public record Response
        {
            public long Id { get; init; }
            public string OrderNumber { get; init; } = string.Empty;
            public long UserId { get; init; }
            public OrderStatus Status { get; init; }
            public decimal Subtotal { get; init; }
            public decimal TaxAmount { get; init; }
            public decimal ShippingAmount { get; init; }
            public decimal DiscountAmount { get; init; }
            public decimal TotalAmount { get; init; }
            public string Currency { get; init; } = "USD";
            public UserAddressResponseDto? ShippingAddress { get; init; }
            public UserAddressResponseDto? BillingAddress { get; init; }
            public string? Notes { get; init; }
            public DateTime CreatedAt { get; init; }
            public DateTime? UpdatedAt { get; init; }
            public string? QrImageBase64 { get; init; }
            public string? Md5 { get; init; }
            public string? KhqrString { get; init; }
        }

        // ==========================================
        // 5. SUMMARY RESPONSE (For Order History List)
        // ==========================================
        public record SummaryResponse
        {
            public long Id { get; init; }
            public string OrderNumber { get; init; } = string.Empty;
            public OrderStatus Status { get; init; }
            public int TotalItems { get; init; }
            public decimal TotalAmount { get; init; }
            public string Currency { get; init; } = "USD";
            public DateTime CreatedAt { get; init; }
        }
    }
}