using System.Net.Http.Json;
using System.Text.Json.Serialization;
using API_Ecommerce.Models;
using kh.gov.nbc.bakong_khqr;
using kh.gov.nbc.bakong_khqr.model;
using Microsoft.Extensions.Options;

namespace API_Ecommerce.Services
{
    public interface IBakongService
    {
        // 3-argument overload
        (string? qrString, string? md5) GenerateDynamicQr(string orderReference, decimal amount, string currency = "USD");

        // 7-argument overload for individual sellers
        (string? qrString, string? md5) GenerateDynamicQr(
            string orderReference,
            decimal amount,
            string bakongId,
            string merchantName,
            string merchantCity,
            string acquiringId,
            string currency = "USD"
        );

        Task<bool> VerifyTransactionAsync(string md5);
    }

    public class BakongService : IBakongService
    {
        const string BaseUrl = "https://api-bakong.nbc.gov.kh";
        private readonly BakongSettings _settings;
        private readonly HttpClient _httpClient;

        public BakongService(IOptions<BakongSettings> settings, HttpClient httpClient)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
        }

        // Implementation for 3 arguments (falls back to global settings)
        public (string? qrString, string? md5) GenerateDynamicQr(string orderReference, decimal amount, string currency = "USD")
        {
            return GenerateDynamicQr(orderReference, amount, _settings.BakongId, _settings.MerchantName, _settings.MerchantCity, _settings.AcquiringId, currency);
        }

        // Implementation for 7 arguments (used by sellers)
        public (string? qrString, string? md5) GenerateDynamicQr(
            string orderReference,
            decimal amount,
            string bakongId,
            string merchantName,
            string merchantCity,
            string acquiringId,
            string currency = "USD")
        {
            var khqrCurrency = currency.ToUpper() == "KHR" ? KHQRCurrency.KHR : KHQRCurrency.USD;
            long expirationTime = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds();

            var merchantInfo = new MerchantInfo
            {
                BakongAccountID = bakongId,
                MerchantID = "123456",
                AcquiringBank = acquiringId,
                Currency = khqrCurrency,
                Amount = (double)amount,
                MerchantName = merchantName,
                MerchantCity = merchantCity,
                BillNumber = orderReference,
                StoreLabel = "Online Store",
                TerminalLabel = "Web Checkout",
                ExpirationTimestamp = expirationTime
            };

            var response = BakongKHQR.GenerateMerchant(merchantInfo);

            if (response.Status.Code != 0)
            {
                Console.WriteLine($"[BAKONG ERROR] Code: {response.Status.Code} | Message: {response.Status.Message}");
            }

            if (response.Status.Code == 0 && response.Data != null)
            {
                return (response.Data.QR, response.Data.MD5);
            }

            return (null, null);
        }

        public async Task<bool> VerifyTransactionAsync(string md5)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.Token);

                var payload = new { md5 = md5 };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/v1/check_transaction_by_md5", payload);

                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<BakongCheckResponse>();

                return result != null && result.ResponseCode == 0 && result.Data != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public class BakongCheckResponse
    {
        [JsonPropertyName("responseCode")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("responseMessage")]
        public string? ResponseMessage { get; set; }

        [JsonPropertyName("data")]
        public BakongTransactionData? Data { get; set; }
    }

    public class BakongTransactionData
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("fromAccountId")]
        public string? FromAccountId { get; set; }

        [JsonPropertyName("toAccountId")]
        public string? ToAccountId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}