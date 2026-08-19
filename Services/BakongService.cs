using System.Net.Http.Headers;
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
        (string? qrString, string? md5) GenerateDynamicQr(string orderReference, decimal amount, string currency = "USD");

        (string? qrString, string? md5) GenerateDynamicQr(
            string orderReference,
            decimal amount,
            string bakongId,
            string merchantName,
            string merchantCity,
            string acquiringId,
            string currency = "USD"
        );

        Task<(bool IsPaid, string? RawResponse)> VerifyTransactionAsync(string md5, string? customToken = null);
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

        public (string? qrString, string? md5) GenerateDynamicQr(string orderReference, decimal amount, string currency = "USD")
        {
            return GenerateDynamicQr(orderReference, amount, _settings.BakongId, _settings.MerchantName, _settings.MerchantCity, _settings.AcquiringId, currency);
        }

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

            var individualInfo = new IndividualInfo
            {
                BakongAccountID = bakongId,
                Currency = khqrCurrency,
                Amount = (double)amount,
                MerchantName = merchantName,
                MerchantCity = merchantCity,
                BillNumber = orderReference,
                StoreLabel = "Online Store",
                TerminalLabel = "Web Checkout",
                ExpirationTimestamp = expirationTime
            };

            var response = BakongKHQR.GenerateIndividual(individualInfo);

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

        public async Task<(bool IsPaid, string? RawResponse)> VerifyTransactionAsync(string md5, string? customToken = null)
        {
            try
            {
                var tokenToUse = !string.IsNullOrWhiteSpace(customToken) ? customToken : _settings.Token;

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/check_transaction_by_md5");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenToUse);
                request.Content = JsonContent.Create(new { md5 = md5 });

                var response = await _httpClient.SendAsync(request);
                var rawBody = await response.Content.ReadAsStringAsync();

                // 🔍 PRINT BAKONG RESPONSE DIRECTLY TO CONSOLE FOR DEBUGGING
                Console.WriteLine($"[BAKONG API RESPONSE] MD5: {md5} | Status: {response.StatusCode} | Body: {rawBody}");

                if (!response.IsSuccessStatusCode)
                {
                    return (false, rawBody);
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<BakongCheckResponse>(rawBody);
                bool isPaid = result != null && result.ResponseCode == 0 && result.Data != null;

                return (isPaid, rawBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BAKONG EXCEPTION]: {ex.Message}");
                return (false, ex.Message);
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