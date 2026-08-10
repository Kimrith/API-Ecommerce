using API_Ecommerce.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/GenerateQROrders")]
    [Authorize]
    public class GenerateOrderController : ControllerBase
    {
        private readonly GenerateOrderQrQuery _generateOrderQrQuery;

        public GenerateOrderController(GenerateOrderQrQuery generateOrderQrQuery)
        {
            _generateOrderQrQuery = generateOrderQrQuery;
        }

        [HttpGet("{id}/generate-qr")]
        public async Task<IActionResult> GenerateQr(long id)
        {
            try
            {
                var result = await _generateOrderQrQuery.ExecuteAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "QR code generated successfully.",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while generating the QR code.", error = ex.Message });
            }
        }
    }
}