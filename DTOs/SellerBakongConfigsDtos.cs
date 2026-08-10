namespace API_Ecommerce.DTOs
{
    public class SellerBakongConfigsDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string BakongId { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string MerchantCity { get; set; } = string.Empty;
        public string AcquiringId { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string? Token { get; set; }

        // New fields for the QR representation
        public string? QrString { get; set; }
        public string? QrImageBase64 { get; set; }
    }

    public class UpsertSellerBakongConfigDto
    {
        public string BakongId { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string MerchantCity { get; set; } = string.Empty;
        public string AcquiringId { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = "https://api-bakong.nbc.gov.kh";
        public string? Token { get; set; }
    }
}