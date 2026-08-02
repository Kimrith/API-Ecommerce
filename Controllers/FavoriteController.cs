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
    // [Authorize] // Uncomment when authentication is active
    public class FavoriteController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly FavoriteQueries _favoriteQueries;

        public FavoriteController(IMediator mediator, FavoriteQueries favoriteQueries)
        {
            _mediator = mediator;
            _favoriteQueries = favoriteQueries;
        }

        // Helper to extract User ID from JWT Claims if available, fallback for testing
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return 1; // Default fallback for development/testing if token isn't fully set up
        }

        // GET: api/favorite/user/{userId}
        [HttpGet("user/{userId:long}")]
        public async Task<ActionResult<IEnumerable<FavoriteResponseDto>>> GetUserFavorites(long userId)
        {
            var favorites = await _favoriteQueries.GetFavoritesByUserIdAsync(userId);
            return Ok(favorites);
        }

        // POST: api/favorite
        [HttpPost]
        public async Task<ActionResult<FavoriteResponseDto>> AddFavorite([FromBody] CreateFavoriteDto dto)
        {
            long userId = GetCurrentUserId(); // Extract from token or use fallback

            var command = new CreateFavoriteCommand(userId, dto.ProductId);
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetUserFavorites), new { userId = userId }, result);
        }

        // DELETE: api/favorite/product/{productId}
        [HttpDelete("product/{productId:long}")]
        public async Task<IActionResult> RemoveFavorite(long productId)
        {
            long userId = GetCurrentUserId();

            var command = new DeleteFavoriteCommand(userId, productId);
            var deleted = await _mediator.Send(command);

            if (!deleted)
            {
                return NotFound(new { message = "Favorite item not found." });
            }

            return NoContent();
        }
    }
}