using API_Ecommerce.Commands;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires valid JWT authentication
    public class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly CategoriesQueries _queries;

        public CategoriesController(IMediator mediator, CategoriesQueries queries)
        {
            _mediator = mediator;
            _queries = queries;
        }

        // --- 1. Get All Categories (Public / Authenticated) ---
        // GET: api/categories?status=Approved
        // --- 1. Get All Categories (Public / Authenticated) ---
        // GET: api/categories?pageNumber=1&pageSize=10&status=Approved
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories(
            [FromQuery] PaginationParamsDtos paginationParams,
            [FromQuery] CategoriesStatus? status)
        {
            var categories = await _queries.GetAllCategoriesAsync(paginationParams, status);
            return Ok(categories);
        }

        // --- 2. Get Category By ID ---
        // GET: api/categories/5
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _queries.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { message = $"Category with ID {id} was not found." });
            }

            return Ok(category);
        }

        // --- 3. Get Categories Created by a Specific Seller (Paginated) ---
        // GET: api/categories/seller/5?pageNumber=1&pageSize=10&status=Approved&searchTerm=electronics
        [HttpGet("seller/{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> GetCategoriesBySellerId(
            int id,
            [FromQuery] PaginationParamsDtos paginationParams,
            [FromQuery] CategoriesStatus? status,
            [FromQuery] string? searchTerm)
        {
            var categories = await _queries.GetCategoriesByUserIdPagedAsync(id, paginationParams, status, searchTerm);
            return Ok(categories);
        }

        // --- 4. Create Category ---
        // POST: api/categories
        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        [Consumes("multipart/form-data")] // Fixed double bracket typo
        public async Task<IActionResult> CreateCategory([FromForm] CreateCategoryDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _mediator.Send(new CreateCategoriesCommand(dto, userId, userRole));

            return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, result);
        }

        // --- 5. Update Category Details & Image ---
        // PUT: api/categories/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        [Consumes("multipart/form-data")] // Added for Form File upload
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateCategoryDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _mediator.Send(new UpdateCategoriesCommand(id, dto, userId, userRole));

            return Ok(result);
        }

        // --- 6. Suspend / Change Status of Category ---
        // PATCH: api/categories/5/status
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> UpdateCategoryStatus(int id, [FromBody] UpdateCategoryStatusDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _mediator.Send(new SuspendCategoriesCommand(id, dto.Status, userId, userRole));

            return Ok(result);
        }

        // --- 7. Delete Category ---
        // DELETE: api/categories/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            await _mediator.Send(new DeleteCategoriesCommand(id, userId, userRole));

            return NoContent(); // 204 No Content
        }

        // --- 8. Get Categories Statistics ---
        // GET: api/categories/statistics
        [HttpGet("statistics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoriesStatistics([FromQuery] int? sellerId)
        {
            var statistics = await _queries.GetCategoriesStatisticsAsync(sellerId);
            return Ok(statistics);
        }

        // --- Private Helper Methods to Extract Claims ---
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User ID claim missing or invalid in JWT token.");
            }

            return userId;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        }
    }
}