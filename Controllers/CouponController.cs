using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.Models;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication for endpoints by default
    public class CouponController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CouponController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // =========================================================
        // 1. GET ALL COUPONS (Admin or Sellers)
        // GET: api/Coupon
        // =========================================================
        [HttpGet]
        [Authorize(Roles = "Admin,Seller")]
        [ProducesResponseType(typeof(List<Coupon>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCoupons()
        {
            var query = new CouponQueries.GetAllCouponsQuery();
            var coupons = await _mediator.Send(query);
            return Ok(coupons);
        }

        // =========================================================
        // 2. GET COUPON BY ID
        // GET: api/Coupon/{id}
        // =========================================================
        [HttpGet("{id:long}")]
        [Authorize(Roles = "Admin,Seller")]
        [ProducesResponseType(typeof(Coupon), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCouponById(long id)
        {
            var query = new CouponQueries.GetCouponByIdQuery(id);
            var coupon = await _mediator.Send(query);

            if (coupon == null)
            {
                return NotFound(new { message = $"Coupon with ID {id} was not found." });
            }

            return Ok(coupon);
        }

        // =========================================================
        // 3. GET COUPON BY CODE (Useful for validation/lookup)
        // GET: api/Coupon/code/{code}
        // =========================================================
        [HttpGet("code/{code}")]
        [ProducesResponseType(typeof(Coupon), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCouponByCode(string code)
        {
            var query = new CouponQueries.GetCouponByCodeQuery(code);
            var coupon = await _mediator.Send(query);

            if (coupon == null)
            {
                return NotFound(new { message = $"Coupon with code '{code}' was not found." });
            }

            return Ok(coupon);
        }

        // =========================================================
        // 4. CREATE A NEW COUPON (Admin Only)
        // POST: api/Coupon
        // =========================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Coupon), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponCommand command)
        {
            try
            {
                var createdCoupon = await _mediator.Send(command);
                return CreatedAtAction(
                    nameof(GetCouponById),
                    new { id = createdCoupon.Id },
                    createdCoupon
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // =========================================================
        // 5. UPDATE AN EXISTING COUPON (Admin Only)
        // PUT: api/Coupon/{id}
        // =========================================================
        [HttpPut("{id:long}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Coupon), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCoupon(long id, [FromBody] UpdateCouponCommand command)
        {
            if (id != command.Id)
            {
                command = command with { Id = id };
            }

            try
            {
                var updatedCoupon = await _mediator.Send(command);
                return Ok(updatedCoupon);
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
        // 6. DELETE A COUPON (Admin Only)
        // DELETE: api/Coupon/{id}
        // =========================================================
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCoupon(long id)
        {
            try
            {
                var command = new DeleteCouponCommand(id);
                await _mediator.Send(command);
                return Ok(new { message = $"Coupon with ID {id} was deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}