using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using API_Ecommerce.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderQueries _orderQueries;
        private readonly CreateOrderCommand _createOrderCommand;
        private readonly UpdateOrderStatusCommandHandler _updateOrderStatusHandler;

        public OrderController(
            OrderQueries orderQueries,
            CreateOrderCommand createOrderCommand,
            UpdateOrderStatusCommandHandler updateOrderStatusHandler)
        {
            _orderQueries = orderQueries;
            _createOrderCommand = createOrderCommand;
            _updateOrderStatusHandler = updateOrderStatusHandler;
        }

        // ==========================================
        // 1. POST: api/Order (Create Order / Checkout)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(OrderDtos.Response), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDtos.CheckoutRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { message = "Invalid or missing user token." });
                }

                var orderResponse = await _createOrderCommand.ExecuteAsync(
                    userId,
                    request.OrderDetails,
                    request.CartItems,
                    request.Currency ?? "USD"
                );

                return CreatedAtAction(
                    nameof(GetOrderById),
                    new { id = orderResponse.Id },
                    orderResponse
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while processing your order.", details = ex.Message });
            }
        }

        // ==========================================
        // 2. GET: api/Order/{id} (Fetch single order)
        // ==========================================
        [HttpGet("{id:long}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderDtos.Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(long id)
        {
            var order = await _orderQueries.GetOrderByIdAsync(id);

            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} was not found." });
            }

            return Ok(order);
        }

        // ==========================================
        // 3. GET: api/Order
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllOrders([FromQuery] PaginationParamsDtos paginationParams)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

            long? userId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out long parsedUserId))
            {
                userId = parsedUserId;
            }

            var result = await _orderQueries.GetAllOrdersAsync(userId, userRole, paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Get paginated orders containing products for a specific seller ID.
        /// </summary>
        [HttpGet("seller/{sellerId:long}")]
        public async Task<IActionResult> GetOrdersBySellerId(long sellerId, [FromQuery] PaginationParamsDtos paginationParams)
        {
            var result = await _orderQueries.GetOrdersBySellerIdAsync(sellerId, paginationParams);
            return Ok(result);
        }

        // ==========================================
        // 3b. GET: api/Order/user/{userId} (Fetch orders for specific customer)
        // ==========================================
        [HttpGet("user/{userId:long}")]
        [Authorize]
        public async Task<IActionResult> GetOrdersByUserId(long userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long currentUserId))
            {
                return Unauthorized(new { message = "Invalid or missing user token." });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
            if (currentUserId != userId && userRole != "Admin")
            {
                return Forbid();
            }

            var orders = await _orderQueries.GetOrdersByUserIdAsync(userId);
            return Ok(orders);
        }

        // ==========================================
        // 4. GET: api/Order/statistics
        // ==========================================
        [HttpGet("statistics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderStatistics([FromQuery] long? sellerId)
        {
            var statistics = await _orderQueries.GetOrderStatisticsAsync(sellerId);
            return Ok(statistics);
        }

        // ==========================================
        // 5. PATCH: api/Order/{id}/status
        // ==========================================
        [HttpPatch("{id:long}/status")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateOrderStatus(long id, [FromBody] OrderDtos.UpdateStatus dto)
        {
            var (success, message, data) = await _updateOrderStatusHandler.HandleAsync(id, dto);

            if (!success)
            {
                return NotFound(new { message });
            }

            return Ok(data);
        }
    }
}