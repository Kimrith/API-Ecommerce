using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartItemController : ControllerBase
    {
        private readonly IMediator _mediator;
        private const string GuestSessionCookieName = "GuestSessionId";

        public CartItemController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // --- 1. GET /api/cart ---
        [HttpGet]
        public async Task<ActionResult<CartDtos.Response>> GetCart()
        {
            var (userId, sessionId) = GetUserOrSessionIdentity();

            // If user is unauthenticated AND has no guest session cookie yet,
            // return an empty cart DTO without throwing an exception.
            if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
            {
                return Ok(new CartDtos.Response());
            }

            var query = new GetCartQuery(userId, sessionId);
            var response = await _mediator.Send(query);

            return Ok(response ?? new CartDtos.Response());
        }

        // --- 2. POST /api/cart/items ---
        [HttpPost("items")]
        public async Task<ActionResult<CartDtos.Response>> AddItem([FromBody] CartItemDtos.Create dto)
        {
            // Set ensureGuestSession to true so a cookie is created when adding an item
            var (userId, sessionId) = GetUserOrSessionIdentity(ensureGuestSession: true);

            var command = new CreateCartItemCommand(userId, sessionId, dto);
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        // --- 3. PUT /api/cart/items/{cartItemId} ---
        [HttpPut("items/{cartItemId:long}")]
        public async Task<ActionResult<CartDtos.Response>> UpdateItemQuantity(
            long cartItemId,
            [FromBody] CartItemDtos.UpdateQuantity dto)
        {
            var (userId, sessionId) = GetUserOrSessionIdentity();

            if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("No active cart session found.");
            }

            var command = new UpdateCartItemCommand(userId, sessionId, cartItemId, dto);
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        // --- 4. DELETE /api/cart/items/{cartItemId} ---
        [HttpDelete("items/{cartItemId:long}")]
        public async Task<ActionResult<CartDtos.Response>> DeleteItem(long cartItemId)
        {
            var (userId, sessionId) = GetUserOrSessionIdentity();

            if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("No active cart session found.");
            }

            var command = new DeleteCartItemCommand(userId, sessionId, cartItemId);
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        // --- HELPER METHOD ---
        private (long? UserId, string? SessionId) GetUserOrSessionIdentity(bool ensureGuestSession = false)
        {
            long? userId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (long.TryParse(nameIdentifier, out var parsedId))
                {
                    userId = parsedId;
                }
            }

            string? sessionId = Request.Cookies[GuestSessionCookieName];

            if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId) && ensureGuestSession)
            {
                sessionId = Guid.NewGuid().ToString("N");
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(30)
                };

                Response.Cookies.Append(GuestSessionCookieName, sessionId, cookieOptions);
            }

            return (userId, sessionId);
        }
    }
}