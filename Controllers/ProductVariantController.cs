using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductVariantController : ControllerBase
    {
        private readonly ProductVariantQueries _variantQueries;
        private readonly IMediator _mediator;

        public ProductVariantController(ProductVariantQueries variantQueries, IMediator mediator)
        {
            _variantQueries = variantQueries;
            _mediator = mediator;
        }

        // ==========================================
        // QUERIES (GET Endpoints)
        // ==========================================

        /// <summary>
        /// Gets all variants for a specific product.
        /// Public users only see available variants (Stock > 0).
        /// Admins and Sellers can see all variants (including out-of-stock / suspended).
        /// </summary>
        [HttpGet("product/{productId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductVariantResponseDto>>> GetByProductId(
            int productId,
            [FromQuery] bool includeSuspended = false)
        {
            bool isAuthorizedStaff = User.IsInRole("Admin") || User.IsInRole("Seller");
            bool showAll = isAuthorizedStaff && includeSuspended;

            var variants = await _variantQueries.GetByProductIdAsync(productId, showAll);
            return Ok(variants);
        }

        /// <summary>
        /// Gets a single product variant by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductVariantResponseDto>> GetById(int id)
        {
            var variant = await _variantQueries.GetByIdAsync(id);
            if (variant == null)
            {
                return NotFound(new { message = $"Product variant with ID {id} was not found." });
            }

            return Ok(variant);
        }

        /// <summary>
        /// Gets a single product variant by SKU code.
        /// </summary>
        [HttpGet("sku/{sku}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductVariantResponseDto>> GetBySku(string sku)
        {
            var variant = await _variantQueries.GetBySkuAsync(sku);
            if (variant == null)
            {
                return NotFound(new { message = $"Product variant with SKU '{sku}' was not found." });
            }

            return Ok(variant);
        }

        /// <summary>
        /// Helper endpoint to check if a SKU is already taken before submitting forms.
        /// </summary>
        [HttpGet("check-sku")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<ActionResult<bool>> CheckSku([FromQuery] string sku, [FromQuery] int? excludeVariantId = null)
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                return BadRequest(new { message = "SKU parameter is required." });
            }

            bool exists = await _variantQueries.SkuExistsAsync(sku, excludeVariantId);
            return Ok(new { sku, exists });
        }

        // ==========================================
        // COMMANDS (POST, PUT, DELETE Endpoints)
        // ==========================================

        /// <summary>
        /// Creates a new product variant.
        /// Requires Admin or Seller privileges.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<ActionResult<ProductVariantResponseDto>> Create([FromBody] CreateProductVariantDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new CreateProductVariantCommand(dto);
            var result = await _mediator.Send(command);

            if (result == null)
            {
                return BadRequest(new { message = "Failed to create variant. Parent product may not exist or SKU is already in use." });
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Updates an existing product variant.
        /// Requires Admin or Seller privileges.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<ActionResult<ProductVariantResponseDto>> Update(int id, [FromBody] UpdateProductVariantDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = new UpdateProductVariantCommand(id, dto);
            var result = await _mediator.Send(command);

            if (result == null)
            {
                return NotFound(new { message = $"Product variant with ID {id} was not found or update failed." });
            }

            return Ok(result);
        }

        /// <summary>
        /// Deletes a product variant by ID.
        /// Requires Admin or Seller privileges.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> Delete(int id)
        {
            var command = new DeleteProductVariantCommand(id);
            bool success = await _mediator.Send(command);

            if (!success)
            {
                return NotFound(new { message = $"Product variant with ID {id} was not found or could not be deleted." });
            }

            return NoContent();
        }
    }
}