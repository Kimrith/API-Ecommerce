namespace API_Ecommerce.Models
{
    public class SellerBakongConfigs
    {
        public int Id { get; set; }

        public int SellerId { get; set; }

        public string BakongId { get; set; } = string.Empty;

        public string MerchantName { get; set; } = string.Empty;

        public string MerchantCity { get; set; } = string.Empty;

        public string AcquiringId { get; set; } = string.Empty;

        public string ApiBaseUrl { get; set; } = "https://api-bakong.nbc.gov.kh";

        public string? Token { get; set; }
    }
}