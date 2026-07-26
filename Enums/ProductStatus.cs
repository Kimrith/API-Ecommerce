namespace API_Ecommerce.Enums
{
    public enum ProductStatus
    {
        Draft = 0,       // Seller is still editing
        Pending = 1,     // Waiting for Admin approval
        Approved = 2,    // Live on Customer store
        Rejected = 3,    // Rejected by Admin
        Archived = 4,     // Hidden / Out of stock
        Suspended = 5,  // Suspended by Admin/Seller
    }
}