namespace API_Ecommerce.DTOs
{
    public class CheckoutRequestDto
    {
        public List<CheckoutCartItemDto> Items { get; set; } = new();
    }

    public class CheckoutCartItemDto
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}