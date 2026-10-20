using API_Ecommerce.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderItemsController : ControllerBase
    {
        private readonly OrderItemQueries _orderItemQueries;
        private readonly OrderQueries _orderQueries;

        public OrderItemsController(OrderItemQueries orderItemQueries, OrderQueries orderQueries)
        {
            _orderItemQueries = orderItemQueries;
            _orderQueries = orderQueries;
        }

        [HttpGet("my-purchased-products")]
        public async Task<IActionResult> GetMyPurchasedProducts()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var items = await _orderItemQueries.GetPurchasedItemsByUserIdAsync(userId);
            return Ok(items);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetItemsByOrder(long orderId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var order = await _orderQueries.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {orderId} was not found." });
            }

            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
            if (order.UserId != currentUserId && userRole != "Admin" && userRole != "Seller")
            {
                return Forbid();
            }

            var items = await _orderItemQueries.GetItemsByOrderIdAsync(orderId);
            return Ok(items);
        }
    }
}