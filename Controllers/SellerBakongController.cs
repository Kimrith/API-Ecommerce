using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using API_Ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly AppDbContext _context;

        public SellerBakongController(
            ISellerBakongService bakongService,
            IBakongService bakongKhqrService,
            AppDbContext context)
        {
            _bakongService = bakongService;
            _bakongKhqrService = bakongKhqrService;
            _context = context;
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

        [HttpPost("generate-qr-from-cart")]
        public async Task<IActionResult> GenerateQrFromCart([FromQuery] long userId)
        {
            try
            {
                int sellerId = GetCurrentSellerId();

                // 1. Fetch the cart and its items safely without strict product navigation requirement
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Variant)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
                {
                    return BadRequest(new { message = "The cart is empty or not found." });
                }

                // 2. Calculate total amount directly from cart items
                decimal totalAmount = cart.CartItems.Sum(item => item.Quantity * item.Price);

                if (totalAmount <= 0)
                {
                    return BadRequest(new { message = "Invalid cart total amount." });
                }

                // 3. Get Seller Bakong Configuration
                var config = await _bakongService.GetConfigBySellerIdAsync(sellerId);
                if (config == null || string.IsNullOrEmpty(config.BakongId))
                {
                    return BadRequest(new { message = "Please set up your Bakong KHQR configuration first." });
                }

                string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";

                // 4. Generate Dynamic KHQR string and MD5 hash
                var (qrString, md5) = _bakongKhqrService.GenerateDynamicQr(
                    orderNumber,
                    totalAmount,
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

                // 5. Create and Save to your Order & OrderItem tables
                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = orderNumber,
                    Status = OrderStatus.Pending,
                    Subtotal = totalAmount,
                    TotalAmount = totalAmount,
                    Currency = "USD",
                    CreatedAt = DateTime.UtcNow,
                    OrderItems = cart.CartItems.Select(ci => new OrderItem
                    {
                        ProductId = ci.ProductId,
                        VariantId = ci.VariantId,
                        ProductName = "Product #" + ci.ProductId, // Fallback safe name if product join isn't loaded
                        VariantName = ci.Variant?.Title,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.Price,
                        TotalPrice = ci.Quantity * ci.Price
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                string qrImageBase64 = GenerateQrBase64(qrString);

                return Ok(new
                {
                    orderNumber = orderNumber,
                    totalAmount = totalAmount,
                    itemsCount = cart.CartItems.Count,
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