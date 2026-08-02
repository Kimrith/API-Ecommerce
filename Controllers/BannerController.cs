using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API_Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannerController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly BannerQueries _bannerQueries;

        public BannerController(IMediator mediator, BannerQueries bannerQueries)
        {
            _mediator = mediator;
            _bannerQueries = bannerQueries;
        }

        // GET: api/banner
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BannerResponseDto>>> GetAllBanners(
            [FromQuery] string? position = null,
            [FromQuery] bool? isActiveOnly = null)
        {
            var banners = await _bannerQueries.GetAllBannersAsync(position, isActiveOnly);
            return Ok(banners);
        }

        // GET: api/banner/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<BannerResponseDto>> GetBannerById(long id)
        {
            var banner = await _bannerQueries.GetByIdAsync(id);
            if (banner == null)
            {
                return NotFound(new { message = "Banner not found." });
            }
            return Ok(banner);
        }

        // POST: api/banner
        [HttpPost]
        public async Task<ActionResult<BannerResponseDto>> CreateBanner([FromForm] CreateBannerDto dto)
        {
            var command = new CreateBannerCommand(dto);
            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetBannerById), new { id = result.Id }, result);
        }

        // PUT: api/banner/{id}
        [HttpPut("{id:long}")]
        public async Task<ActionResult<BannerResponseDto>> UpdateBanner(long id, [FromForm] UpdateBannerDto dto)
        {
            var command = new UpdateBannerCommand(id, dto);
            var result = await _mediator.Send(command);

            if (result == null)
            {
                return NotFound(new { message = "Banner not found." });
            }

            return Ok(result);
        }

        // DELETE: api/banner/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteBanner(long id)
        {
            var command = new DeleteBannerCommand(id);
            var deleted = await _mediator.Send(command);

            if (!deleted)
            {
                return NotFound(new { message = "Banner not found." });
            }

            return NoContent();
        }
    }
}