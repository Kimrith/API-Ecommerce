using API_Ecommerce.DTOs;
using API_Ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Seller")]
    public class SellerBakongController : ControllerBase
    {
        private readonly ISellerBakongService _bakongService;
        private readonly IBakongService _bakongKhqrService;

        public SellerBakongController(ISellerBakongService bakongService, IBakongService bakongKhqrService)
        {
            _bakongService = bakongService;
            _bakongKhqrService = bakongKhqrService;
        }

        private int GetCurrentSellerId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int sellerId))
            {
                throw new UnauthorizedAccessException("Invalid token or seller ID not found.");
            }
            return sellerId;
        }

        [HttpGet("generate-qr")]
        public async Task<IActionResult> GenerateMyQrCode([FromQuery] decimal amount)
        {
            try
            {
                int sellerId = GetCurrentSellerId();
                var config = await _bakongService.GetConfigBySellerIdAsync(sellerId);

                if (config == null || string.IsNullOrEmpty(config.BakongId))
                {
                    return BadRequest(new { message = "Please set up your Bakong KHQR configuration first." });
                }

                string billReference = $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}";

                var (qrString, md5) = _bakongKhqrService.GenerateDynamicQr(
                    billReference,
                    amount,
                    config.BakongId,
                    config.MerchantName,
                    config.MerchantCity,
                    config.AcquiringId,
                    "USD"
                );

                if (string.IsNullOrEmpty(qrString))
                {
                    return BadRequest(new { message = "Failed to generate Bakong KHQR string." });
                }

                string qrImageBase64 = GenerateQrBase64(qrString);

                return Ok(new
                {
                    sellerId = sellerId,
                    bakongId = config.BakongId,
                    merchantName = config.MerchantName,
                    amount = amount,
                    khqrString = qrString,
                    qrImageBase64 = qrImageBase64,
                    md5 = md5
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        private static string GenerateQrBase64(string qrString)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);

            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyConfig()
        {
            try
            {
                int sellerId = GetCurrentSellerId();
                var config = await _bakongService.GetConfigBySellerIdAsync(sellerId);

                if (config == null)
                    return NotFound(new { message = "KHQR configuration not found. Please set up your payment details." });

                return Ok(config);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("me")]
        public async Task<IActionResult> UpsertMyConfig([FromBody] UpsertSellerBakongConfigDto dto)
        {
            try
            {
                int sellerId = GetCurrentSellerId();
                var result = await _bakongService.UpsertConfigAsync(sellerId, dto);
                return Ok(new { message = "KHQR configuration saved successfully.", data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while saving configuration.", error = ex.Message });
            }
        }
    }
}