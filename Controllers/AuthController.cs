using API_Ecommerce.Commands.Create;
using API_Ecommerce.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CreateAuthCommand _authCommand;

        public AuthController(CreateAuthCommand authCommand)
        {
            _authCommand = authCommand;
        }

        // --- 1. REGISTER (USER / SELLER / ADMIN) ---
        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] RegisterDto dto)
        {
            try
            {
                string? profileImageUrl = null;

                // Save uploaded profile image to wwwroot/uploads if provided
                if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    var extension = Path.GetExtension(dto.ProfileImage.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { message = "Only image files (.jpg, .png, .webp, .gif) are allowed." });
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.ProfileImage.CopyToAsync(stream);
                    }

                    profileImageUrl = $"/uploads/{fileName}";
                }

                // Register user with the role supplied in dto.Role (Customer, Seller, Admin, etc.)
                var response = await _authCommand.ExecuteRegisterAsync(dto, profileImageUrl);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during registration.", details = ex.Message });
            }
        }

        // --- 2. LOGIN ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var response = await _authCommand.ExecuteLoginAsync(dto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login.", details = ex.Message });
            }
        }
    }
}