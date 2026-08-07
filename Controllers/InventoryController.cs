using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetAllInventoryQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:long}", Name = "GetInventoryById")]
        public async Task<ActionResult<InventoryResponseDto>> GetById(long id, CancellationToken cancellationToken)
        {
            var query = new GetInventoryByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<InventoryResponseDto>> Create([FromBody] CreateInventoryDto createDto, CancellationToken cancellationToken)
        {
            var command = new CreateInventoryCommand(createDto);
            var result = await _mediator.Send(command, cancellationToken);

            return CreatedAtRoute("GetInventoryById", new { id = result.Id }, result);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<InventoryResponseDto>> Update(long id, [FromBody] UpdateInventoryDto updateDto, CancellationToken cancellationToken)
        {
            var command = new UpdateInventoryCommand(id, updateDto);
            var result = await _mediator.Send(command, cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var command = new DeleteInventoryCommand(id);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}