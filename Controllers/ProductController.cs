using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Commands;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ProductQueries _productQueries;

        public ProductController(IMediator mediator, ProductQueries productQueries)
        {
            _mediator = mediator;
            _productQueries = productQueries;
        }

        // =========================================================================
        // READ ENDPOINTS (Raw SQL / Dapper Queries)
        // =========================================================================

        /// <summary>
        /// Get paginated list of products with optional search, category, status, and sorting filters.
        /// Response includes discount schedule and publish status.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResultDto<ProductResponseDto>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? sellerId = null,
            [FromQuery] ProductStatus? status = null,
            [FromQuery] string? sortBy = null)
        {
            var result = await _productQueries.GetAllProductsAsync(
                pageNumber, pageSize, searchTerm, categoryId, sellerId, status, sortBy);

            return Ok(result);
        }

        /// <summary>
        /// Get product details by ID (includes discount pricing and publication schedule).
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductResponseDto>> GetById(int id)
        {
            var product = await _productQueries.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} was not found." });
            }

            return Ok(product);
        }

        /// <summary>
        /// Get product details by URL slug (includes discount pricing and publication schedule).
        /// </summary>
        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductResponseDto>> GetBySlug(string slug)
        {
            var product = await _productQueries.GetBySlugAsync(slug);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with slug '{slug}' was not found." });
            }

            return Ok(product);
        }

        /// <summary>
        /// Get paginated products created by a specific seller ID with optional filters.
        /// </summary>
        [HttpGet("seller/{sellerId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResultDto<ProductResponseDto>>> GetBySellerId(
            int sellerId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? sortBy = null)
        {
            var result = await _productQueries.GetProductsBySellerPagedAsync(
                sellerId, pageNumber, pageSize, searchTerm, categoryId, sortBy);

            return Ok(result);
        }

        // =========================================================================
        // WRITE ENDPOINTS (MediatR Commands)
        // =========================================================================

        /// <summary>
        /// Create a new product with an optional image upload, discount settings, and scheduled publish date.
        /// </summary>
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductResponseDto>> Create([FromForm] CreateProductDto dto)
        {
            int userId = GetCurrentUserId();
            string userRole = GetCurrentUserRole();

            var command = new CreateProductCommand(dto, userId, userRole);
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Update an existing product (supports updating discount pricing, scheduled publish date, and replacing image files).
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductResponseDto>> Update(int id, [FromForm] UpdateProductDto dto)
        {
            int userId = GetCurrentUserId();
            string userRole = GetCurrentUserRole();

            try
            {
                var command = new UpdateProductCommand(id, dto, userId, userRole);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        /// <summary>
        /// Suspend a product listing (Admin or Product Owner).
        /// </summary>
        [HttpPatch("{id:int}/suspend")]
        [Authorize]
        public async Task<ActionResult<ProductResponseDto>> Suspend(int id)
        {
            int userId = GetCurrentUserId();
            string userRole = GetCurrentUserRole();

            try
            {
                var command = new SuspendProductCommand(id, ProductStatus.Suspended, userId, userRole);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update a product's status (Admin or Product Owner).
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize]
        public async Task<ActionResult<ProductResponseDto>> UpdateStatus(int id, [FromBody] UpdateProductStatusDto dto)
        {
            int userId = GetCurrentUserId();
            string userRole = GetCurrentUserRole();

            try
            {
                var command = new SuspendProductCommand(id, dto.Status, userId, userRole);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Permanently delete a product and remove its image from local storage.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = GetCurrentUserId();
            string userRole = GetCurrentUserRole();

            try
            {
                var command = new DeleteProductCommand(id, userId, userRole);
                await _mediator.Send(command);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<ProductStatisticsDto>> GetStatistics([FromQuery] int? sellerId)
        {
            var stats = await _productQueries.GetProductStatisticsAsync(sellerId);
            return Ok(stats);
        }

        /// <summary>
        /// Get top selling products.
        /// </summary>
        [HttpGet("best-sellers")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TopSellingProductDto>>> GetBestSellers([FromQuery] int limit = 5)
        {
            var products = await _productQueries.GetTopSellingProductsAsync(limit);
            return Ok(products);
        }

        // =========================================================================
        // PRIVATE CLAIMS HELPERS
        // =========================================================================

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("id")?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User ID is missing or invalid in JWT token.");
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? Roles.Seller.ToString();
        }
    }
}