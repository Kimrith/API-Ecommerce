using API_Ecommerce.Enums;

namespace API_Ecommerce.DTOs
{
    public class PaymentDtos
    {
        // 1. DTO for initiating / generating a payment (e.g., generating KHQR)
        public class Create
        {
            public long OrderId { get; set; }
            public string PaymentMethod { get; set; } = "BakongKHQR";
            public decimal Amount { get; set; }
            public string Currency { get; set; } = "USD"; // or "KHR"
        }

        // 2. DTO for verifying a payment (called via webhook or check-status endpoint)
        public class Verify
        {
            public long OrderId { get; set; }
            public string? BakongHash { get; set; }
            public string? ExternalTransactionId { get; set; }
        }

        // 3. DTO for returning payment details to the frontend (e.g., showing the QR code string)
        public class Response
        {
            public long Id { get; set; }
            public long OrderId { get; set; }
            public string PaymentMethod { get; set; } = string.Empty;
            public string? KhqrString { get; set; }
            public string? Md5 { get; set; }
            public decimal Amount { get; set; }
            public string Currency { get; set; } = string.Empty;
            public PaymentStatus Status { get; set; }
            public string? BakongHash { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? PaidAt { get; set; }
        }
    }
}