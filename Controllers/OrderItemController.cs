using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication for all endpoints
    public class OrderItemController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrderItemController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // =========================================================
        // 1. GET ALL ITEMS FOR A SPECIFIC ORDER
        // GET: api/OrderItem/order/{orderId}
        // =========================================================
        [HttpGet("order/{orderId:long}")]
        [ProducesResponseType(typeof(List<OrderItemDtos.Response>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetItemsByOrderId(long orderId)
        {
            var userId = GetCurrentUserId();
            var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Seller");

            // Admins and Sellers pass null to view the order's items; regular users are locked to their own UserId
            var query = new OrderItemQueries.GetOrderItemsByOrderIdQuery(
                orderId,
                isPrivileged ? null : userId
            );

            var items = await _mediator.Send(query);
            return Ok(items);
        }

        // =========================================================
        // 2. GET SINGLE ORDER ITEM BY ID
        // GET: api/OrderItem/{id}
        // =========================================================
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(OrderItemDtos.Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetItemById(long id)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var query = new OrderItemQueries.GetOrderItemByIdQuery(
                id,
                isAdmin ? null : userId
            );

            var item = await _mediator.Send(query);

            if (item == null)
            {
                return NotFound(new { message = $"OrderItem with ID {id} was not found." });
            }

            return Ok(item);
        }

        // =========================================================
        // 3. ADD ITEM TO AN EXISTING ORDER (Admin / Support Only)
        // POST: api/OrderItem
        // =========================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(OrderDtos.Response), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddItemToOrder([FromBody] CreateOrderItemCommand command)
        {
            try
            {
                var updatedOrder = await _mediator.Send(command);
                return CreatedAtAction(
                    nameof(GetItemsByOrderId),
                    new { orderId = updatedOrder.Id },
                    updatedOrder
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // =========================================================
        // 4. REMOVE AN ITEM FROM AN ORDER (Admin / Support Only)
        // DELETE: api/OrderItem/order/{orderId}/item/{orderItemId}
        // =========================================================
        [HttpDelete("order/{orderId:long}/item/{orderItemId:long}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(OrderDtos.Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOrderItem(long orderId, long orderItemId)
        {
            try
            {
                var command = new DeleteOrderItemCommand(orderId, orderItemId);
                var updatedOrder = await _mediator.Send(command);
                return Ok(updatedOrder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // =========================================================
        // GET: api/OrderItem
        // Gets all order items (Admins see all, Users see only their own)
        // =========================================================
        [HttpGet]
        [ProducesResponseType(typeof(List<OrderItemDtos.Response>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllOrderItems()
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var query = new OrderItemQueries.GetAllOrderItemsQuery(
                isAdmin ? null : userId
            );

            var items = await _mediator.Send(query);
            return Ok(items);
        }

        // =========================================================
        // HELPER METHOD: Extracts authenticated UserId from JWT claims
        // =========================================================
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value
                           ?? User.FindFirst("id")?.Value;

            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Invalid user identity claim in token.");
        }
    }
}