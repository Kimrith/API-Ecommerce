namespace API_Ecommerce.Models
{
    public class CategoriesStatistics
    {
        public int totalCategories { get; set; }
        public int Draft { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Archived { get; set; }
        public int Suspended { get; set; }
    }
}
