using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using API_Ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Seller")]
    public class SellerBakongController : ControllerBase
    {
        private readonly ISellerBakongService _bakongService;
        private readonly AppDbContext _context;

        public SellerBakongController(ISellerBakongService bakongService, AppDbContext context)
        {
            _bakongService = bakongService;
            _context = context;
        }

        private int GetCurrentSellerId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int sellerId))
            {
                throw new UnauthorizedAccessException("Invalid token or seller ID not found.");
            }
            return sellerId;
        }

        // ==========================================
        // 1. GET: api/SellerBakong/me (Fetch Config)
        // ==========================================
        [HttpGet("me")]
        public async Task<IActionResult> GetMyConfig()
        {
            try
            {
                int sellerId = GetCurrentSellerId();
                var config = await _bakongService.GetConfigBySellerIdAsync(sellerId);

                if (config == null)
                    return NotFound(new { message = "KHQR configuration not found. Please set up your payment details." });

                return Ok(config);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        // ==========================================
        // 2. POST: api/SellerBakong/me (Register / Upsert Config)
        // ==========================================
        [HttpPost("me")]
        public async Task<IActionResult> UpsertMyConfig([FromBody] UpsertSellerBakongConfigDto dto)
        {
            try
            {
                int sellerId = GetCurrentSellerId();
                var result = await _bakongService.UpsertConfigAsync(sellerId, dto);

                return Ok(new
                {
                    message = "KHQR configuration saved successfully.",
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while saving configuration.", error = ex.Message });
            }
        }
    }
}