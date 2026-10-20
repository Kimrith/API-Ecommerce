using API_Ecommerce.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/GenerateQROrders")]
    [Authorize]
    public class GenerateOrderController : ControllerBase
    {
        private readonly GenerateOrderQrQuery _generateOrderQrQuery;
        private readonly OrderQueries _orderQueries;

        public GenerateOrderController(GenerateOrderQrQuery generateOrderQrQuery, OrderQueries orderQueries)
        {
            _generateOrderQrQuery = generateOrderQrQuery;
            _orderQueries = orderQueries;
        }

        [HttpGet("{id}/generate-qr")]
        [EnableRateLimiting("CheckoutRateLimit")]
        public async Task<IActionResult> GenerateQr(long id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                               ?? User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
                {
                    return Unauthorized(new { success = false, message = "Invalid or missing user token." });
                }

                var order = await _orderQueries.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound(new { success = false, message = $"Order with ID {id} was not found." });
                }

                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                            ?? User.FindFirst("role")?.Value;
                if (order.UserId != currentUserId && userRole != "Admin")
                {
                    return Forbid();
                }

                var result = await _generateOrderQrQuery.ExecuteAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "QR code generated successfully.",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while generating the QR code." });
            }
        }
    }
}