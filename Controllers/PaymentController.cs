using API_Ecommerce.Commands.Create;
using API_Ecommerce.Commands.Update;
using API_Ecommerce.DTOs;
using API_Ecommerce.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPayments([FromQuery] PaginationParamsDtos paginationParams)
    {
        var result = await _paymentQueries.GetAllPaymentsAsync(paginationParams);
        return Ok(result);
    }

    [HttpGet("statistics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPaymentStatistics()
    {
        var statistics = await _paymentQueries.GetPaymentStatisticsAsync();
        return Ok(statistics);
    }

    [HttpPost("generate-qr-from-cart")]
    public async Task<IActionResult> GenerateQrFromCart([FromBody] CheckoutRequestDto request)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int userId = string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var parsedId) ? 8 : parsedId;

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
    //[Authorize(Roles = "Seller, Admin")]
    public async Task<IActionResult> GetPaymentsBySeller(int sellerId, [FromQuery] PaginationParamsDtos paginationParams)
    {
        var result = await _paymentQueries.GetPaymentsBySellerIdAsync(sellerId, paginationParams);
        return Ok(result);
    }
}