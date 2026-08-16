using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_Ecommerce.Queries;
using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReviewController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/review
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetAllReviews([FromQuery] bool? isApproved)
        {
            var query = new GetAllReviewsQuery { IsApproved = isApproved };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // GET: api/review/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetReviewsByProductId(long productId)
        {
            var query = new GetReviewsByProductIdQuery { ProductId = productId };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // GET: api/review/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ReviewResponseDto>> GetReviewById(long id)
        {
            var query = new GetReviewByIdQuery { Id = id };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(new { message = "Review not found." });
            }

            return Ok(result);
        }

        // GET: api/review/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetReviewsByUserId(long userId)
        {
            var query = new GetReviewsByUserIdQuery { UserId = userId };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // POST: api/review
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReviewResponseDto>> CreateReview([FromBody] CreateReviewDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid token user identifier.");
            }

            var command = new CreateReviewCommand
            {
                ProductId = dto.ProductId,
                UserId = userId,
                Rating = dto.Rating,
                Title = dto.Title,
                Comment = dto.Comment
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetReviewById), new { id = result.Id }, result);
        }

        // PUT: api/review/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ReviewResponseDto>> UpdateReview(long id, [FromBody] UpdateReviewDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid token user identifier.");
            }

            bool isAdmin = User.IsInRole("Admin");

            var command = new UpdateReviewCommand
            {
                Id = id,
                UserId = userId,
                IsAdmin = isAdmin,
                Rating = dto.Rating,
                Title = dto.Title,
                Comment = dto.Comment,
                IsApproved = dto.IsApproved
            };

            try
            {
                var result = await _mediator.Send(command);
                if (result == null)
                {
                    return NotFound(new { message = "Review not found." });
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        // DELETE: api/review/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(long id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid token user identifier.");
            }

            bool isAdmin = User.IsInRole("Admin");

            var command = new DeleteReviewCommand
            {
                Id = id,
                UserId = userId,
                IsAdmin = isAdmin
            };

            try
            {
                var success = await _mediator.Send(command);
                if (!success)
                {
                    return NotFound(new { message = "Review not found." });
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }
    }
}