namespace API_Ecommerce.DTOs
{
    public class OrderItemDtos
    {
        public record Response
        {
            public long Id { get; init; }
            public long ProductId { get; init; }
            public string ProductName { get; init; } = string.Empty;
            public int Quantity { get; init; }
            public decimal UnitPrice { get; init; }
            public decimal TotalPrice { get; init; }
        }

        public record PurchasedProductResponse
        {
            public long Id { get; init; }
            public long ProductId { get; init; }
            public string ProductName { get; init; } = string.Empty;
            public int Quantity { get; init; }
            public decimal UnitPrice { get; init; }
            public decimal TotalPrice { get; init; }
            public string OrderNumber { get; init; } = string.Empty;
            public DateTime PurchasedAt { get; init; }
        }
    }
}