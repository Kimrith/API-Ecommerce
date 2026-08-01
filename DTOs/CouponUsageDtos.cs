namespace API_Ecommerce.DTOs
{
    public class CouponUsageDtos
    {
        // Response DTO returned when viewing coupon usage history or logs
        public class Response
        {
            public long Id { get; set; }
            public long CouponId { get; set; }
            public string CouponCode { get; set; } = string.Empty;
            public long UserId { get; set; }
            public long OrderId { get; set; }
            public DateTime UsedAt { get; set; }
        }
    }
}