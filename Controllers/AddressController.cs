using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Delete;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using Microsoft.AspNetCore.Mvc;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressesController : ControllerBase
    {
        private readonly AddressQueries _queries;
        private readonly CreateAddressCommand _createCommand;
        private readonly UpdateAddressCommand _updateCommand;
        private readonly DeleteAddressCommand _deleteCommand;

        public AddressesController(
            AddressQueries queries,
            CreateAddressCommand createCommand,
            UpdateAddressCommand updateCommand,
            DeleteAddressCommand deleteCommand)
        {
            _queries = queries;
            _createCommand = createCommand;
            _updateCommand = updateCommand;
            _deleteCommand = deleteCommand;
        }

        // GET: api/Addresses/user/5
        [HttpGet("user/{userId:long}")]
        public async Task<IActionResult> GetUserAddresses(long userId)
        {
            var addresses = await _queries.GetAddressesByUserIdAsync(userId);
            return Ok(addresses);
        }

        // GET: api/Addresses/5/user/2
        [HttpGet("{id:long}/user/{userId:long}")]
        public async Task<IActionResult> GetAddressById(long id, long userId)
        {
            var address = await _queries.GetAddressByIdAsync(id, userId);
            if (address == null)
            {
                return NotFound(new { message = "Address not found." });
            }

            return Ok(address);
        }

        // POST: api/Addresses/user/5
        [HttpPost("user/{userId:long}")]
        public async Task<IActionResult> CreateAddress(long userId, [FromBody] CreateAddressDto dto)
        {
            try
            {
                var result = await _createCommand.ExecuteAsync(userId, dto);
                return CreatedAtAction(
                    nameof(GetAddressById),
                    new { id = result.Id, userId = result.UserId },
                    result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Addresses/5/user/2
        [HttpPut("{id:long}/user/{userId:long}")]
        public async Task<IActionResult> UpdateAddress(long id, long userId, [FromBody] UpdateAddressDto dto)
        {
            try
            {
                var result = await _updateCommand.ExecuteAsync(id, userId, dto);
                if (result == null)
                {
                    return NotFound(new { message = "Address not found or unauthorized access." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/Addresses/5/user/2
        [HttpDelete("{id:long}/user/{userId:long}")]
        public async Task<IActionResult> DeleteAddress(long id, long userId)
        {
            var success = await _deleteCommand.ExecuteAsync(id, userId);
            if (!success)
            {
                return NotFound(new { message = "Address not found or unauthorized access." });
            }

            return NoContent();
        }
    }
}