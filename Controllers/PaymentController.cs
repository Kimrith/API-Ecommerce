using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PaymentQueries _paymentQueries;
    private readonly CreatePaymentCommandHandler _createPaymentHandler;
    private readonly VerifyPaymentCommandHandler _verifyPaymentHandler;

    public PaymentController(
        PaymentQueries paymentQueries,
        CreatePaymentCommandHandler createPaymentHandler,
        VerifyPaymentCommandHandler verifyPaymentHandler)
    {
        _paymentQueries = paymentQueries;
        _createPaymentHandler = createPaymentHandler;
        _verifyPaymentHandler = verifyPaymentHandler;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPayments([FromQuery] PaginationParamsDtos paginationParams)
    {
        var result = await _paymentQueries.GetAllPaymentsAsync(paginationParams);
        return Ok(result);
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> GetPaymentStatistics()
    {
        var statistics = await _paymentQueries.GetPaymentStatisticsAsync();
        return Ok(statistics);
    }

    [HttpPost("generate-qr-from-cart")]
    [Authorize]
    [EnableRateLimiting("CheckoutRateLimit")]
    public async Task<IActionResult> GenerateQrFromCart([FromBody] CheckoutRequestDto request)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Invalid or missing user token." });
        }

        var command = new CreatePaymentCommand
        {
            UserId = userId,
            Items = request?.Items ?? new List<CheckoutCartItemDto>()
        };

        var (success, message, data) = await _createPaymentHandler.HandleAsync(command);

        if (!success)
        {
            return BadRequest(new { message });
        }

        return Ok(data);
    }

    // ==========================================
    // Bakong KHQR Webhook Callback (Exempt from Anti-Forgery validation)
    // ==========================================
    [HttpPost("bakong-callback")]
    [AllowAnonymous]
    public IActionResult BakongCallback([FromBody] object payload)
    {
        return Ok(new { success = true, message = "Bakong payment notification received." });
    }

    [HttpGet("verify-payment/{orderId}")]
    public async Task<IActionResult> VerifyPayment(long orderId)
    {
        var command = new VerifyPaymentCommand { OrderId = orderId };
        var (success, status, message) = await _verifyPaymentHandler.HandleAsync(command);

        if (status == "NOT_FOUND")
        {
            return NotFound(new { message });
        }

        return Ok(new { status, message });
    }

    [HttpGet("seller/{sellerId}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> GetPaymentsBySeller(int sellerId, [FromQuery] PaginationParamsDtos paginationParams)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    ?? User.FindFirst("role")?.Value;

        if (userRole == "Seller" && int.TryParse(userIdClaim, out int currentUserId) && currentUserId != sellerId)
        {
            return Forbid();
        }

        var result = await _paymentQueries.GetPaymentsBySellerIdAsync(sellerId, paginationParams);
        return Ok(result);
    }
}
