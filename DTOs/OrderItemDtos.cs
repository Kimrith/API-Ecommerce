namespace API_Ecommerce.DTOs
{
    public class OrderItemDtos
    {
        // ==========================================
        // ORDER ITEM RESPONSE
        // ==========================================
        public record Response
        {
            public long Id { get; init; }
            public long ProductId { get; init; }
            public string ProductName { get; init; } = string.Empty;
            public long? VariantId { get; init; }
            public string? VariantName { get; init; }
            public string? Sku { get; init; }
            public int Quantity { get; init; }
            public decimal UnitPrice { get; init; }
            public decimal TotalPrice { get; init; }
            public DateTime CreatedAt { get; init; }

            // --- Customer Info Fields ---
            public long? UserId { get; init; }
            public string? UserEmail { get; init; } // Adjust property name to match your Auth model columns
        }
    }
}