namespace API_Ecommerce.Enums
{
    public enum PaymentStatus
    {
        Pending = 0,    // KHQR generated, waiting for user scan
        Completed = 1,  // Payment confirmed via Bakong Webhook or CheckTransaction API
        Failed = 2,     // Transaction failed or canceled
        Refunded = 3,    // Payment refunded
        Cancelled = 4
    }
}