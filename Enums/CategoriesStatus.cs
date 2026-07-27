namespace API_Ecommerce.Enums
{
    public enum CategoriesStatus
    {
        Draft = 1,       // Seller is still editing
        Pending = 2,     // Waiting for Admin approval
        Approved = 3,    // Live on Customer store
        Rejected = 4,    // Rejected by Admin
        Archived = 5     // Hidden / Out of stock
    }
}