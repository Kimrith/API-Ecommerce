using API_Ecommerce.Commands;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoriteController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly FavoriteQueries _favoriteQueries;

        public FavoriteController(IMediator mediator, FavoriteQueries favoriteQueries)
        {
            _mediator = mediator;
            _favoriteQueries = favoriteQueries;
        }

        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return null;
        }

        // GET: api/favorite/user/{userId}
        [HttpGet("user/{userId:long}")]
        public async Task<ActionResult<IEnumerable<FavoriteResponseDto>>> GetUserFavorites(long userId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { message = "Invalid or missing user token." });
            }

            if (currentUserId.Value != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var favorites = await _favoriteQueries.GetFavoritesByUserIdAsync(userId);
            return Ok(favorites);
        }

        // POST: api/favorite
        [HttpPost]
        public async Task<ActionResult<FavoriteResponseDto>> AddFavorite([FromBody] CreateFavoriteDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { message = "Invalid or missing user token." });
            }

            try
            {
                var command = new CreateFavoriteCommand(currentUserId.Value, dto.ProductId);
                var result = await _mediator.Send(command);

                return CreatedAtAction(nameof(GetUserFavorites), new { userId = currentUserId.Value }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while adding to favorites." });
            }
        }

        // DELETE: api/favorite/product/{productId}
        [HttpDelete("product/{productId:long}")]
        public async Task<IActionResult> RemoveFavorite(long productId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Unauthorized(new { message = "Invalid or missing user token." });
            }

            var command = new DeleteFavoriteCommand(currentUserId.Value, productId);
            var deleted = await _mediator.Send(command);

            if (!deleted)
            {
                return NotFound(new { message = "Favorite item not found." });
            }

            return NoContent();
        }
    }
}