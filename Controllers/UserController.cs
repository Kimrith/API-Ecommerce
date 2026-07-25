using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using Microsoft.AspNetCore.Mvc;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AuthQueries _authQueries;
        private readonly UpdateAuthCommand _updateAuthCommand;

        public UserController(AuthQueries authQueries, UpdateAuthCommand updateAuthCommand)
        {
            _authQueries = authQueries;
            _updateAuthCommand = updateAuthCommand;
        }

        // --- 1. GET ALL USERS (Optional Filter: GET /api/User?status=Active) ---
        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? status = null)
        {
            try
            {
                var users = await _authQueries.GetAllUsersAsync(status);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching users.", details = ex.Message });
            }
        }

        // --- 2. GET ALL SELLERS (GET /api/User/sellers) ---
        [HttpGet("sellers")]
        public async Task<IActionResult> GetAllSellers()
        {
            try
            {
                var sellers = await _authQueries.GetAllSellersAsync();
                return Ok(sellers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching sellers.", details = ex.Message });
            }
        }

        // --- 3. GET USER BY EMAIL (GET /api/User/by-email?email=test@gmail.com) ---
        [HttpGet("by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string? email) // <-- Add '?' to make it optional
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Email parameter is required." });
                }

                var user = await _authQueries.GetByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new { message = $"User with email '{email}' was not found." });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching the user.", details = ex.Message });
            }
        }

        // --- 4. GET USER BY ID (GET /api/User/1) ---
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _authQueries.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID {id} was not found." });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching the user.", details = ex.Message });
            }
        }
    }
}