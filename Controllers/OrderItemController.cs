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

        public OrderItemsController(OrderItemQueries orderItemQueries)
        {
            _orderItemQueries = orderItemQueries;
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
            var items = await _orderItemQueries.GetItemsByOrderIdAsync(orderId);
            return Ok(items);
        }
    }
}