namespace API_Ecommerce.Models
{
    public class ProductStatistics
    {
        public int TotalProducts { get; set; }
        public int Draft { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Archived { get; set; }
        public int Suspended { get; set; }
    }
}
