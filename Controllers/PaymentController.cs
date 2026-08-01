using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using API_Ecommerce.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder; // Required for QRCoder

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBakongService _bakongService;

    public PaymentController(AppDbContext context, IBakongService bakongService)
    {
        _context = context;
        _bakongService = bakongService;
    }

    [HttpPost("generate-qr-from-cart")]
    public async Task<IActionResult> GenerateQrFromCart()
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            userId = 8; // Fallback for testing
        }

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
        {
            return BadRequest(new { message = "Cart is empty or not found." });
        }

        decimal totalAmount = 0;
        var processedItems = new List<OrderItem>();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                UserId = userId,
                TotalAmount = 0,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var cartItem in cart.CartItems)
            {
                var product = await _context.Products.FindAsync(cartItem.ProductId);
                if (product == null) continue;

                decimal unitPrice = product.Price;
                decimal totalPrice = unitPrice * cartItem.Quantity;
                totalAmount += totalPrice;

                string? variantName = null;
                string? sku = null;

                if (cartItem.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(cartItem.VariantId.Value);
                    if (variant != null)
                    {
                        variantName = null;
                        sku = null;
                    }
                }

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    VariantId = cartItem.VariantId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    ProductName = product.Name,
                    VariantName = variantName,
                    Sku = sku,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderItems.Add(orderItem);
                processedItems.Add(orderItem);
            }

            if (!processedItems.Any())
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "No valid products found in cart." });
            }

            order.TotalAmount = totalAmount;
            await _context.SaveChangesAsync();

            string orderReference = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{order.Id}";
            var (qrString, md5) = _bakongService.GenerateDynamicQr(orderReference, order.TotalAmount, "USD");

            if (string.IsNullOrEmpty(qrString) || string.IsNullOrEmpty(md5))
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "Failed to generate Bakong KHQR." });
            }

            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentMethod = "BakongKHQR",
                Amount = order.TotalAmount,
                Currency = "USD",
                KhqrString = qrString,
                Md5 = md5,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Convert the raw khqrString into a Base64 PNG Image string
            string qrImageBase64 = GenerateQrBase64(qrString);

            return Ok(new
            {
                orderId = order.Id,
                orderReference = orderReference,
                amount = order.TotalAmount,
                khqrString = qrString,
                qrImageBase64 = qrImageBase64, // <-- Ready-to-render Base64 image
                md5 = md5
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "An error occurred while processing the order.", error = ex.Message });
        }
    }

    [HttpGet("verify-payment/{orderId}")]
    public async Task<IActionResult> VerifyPayment(long orderId)
    {
        // Find the payment record associated with this order
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (payment == null)
        {
            return NotFound(new { message = "Payment record not found for this order." });
        }

        // If already paid, return success directly (assuming PaymentStatus has Completed or Paid)
        if (payment.Status == PaymentStatus.Completed) // Or PaymentStatus.Paid depending on your enum
        {
            return Ok(new { status = "PAID", message = "Payment is already completed." });
        }

        // Call Bakong service to check transaction status via MD5
        bool isPaid = await _bakongService.VerifyTransactionAsync(payment.Md5);

        if (isPaid)
        {
            // Update Payment status (Change to PaymentStatus.Paid if Completed doesn't exist on PaymentStatus either)
            payment.Status = PaymentStatus.Completed;

            // Update Order status using a valid enum value (e.g., OrderStatus.Processing, OrderStatus.Paid, or OrderStatus.Confirmed)
            if (payment.Order != null)
            {
                payment.Order.Status = OrderStatus.Processing; // Change this to whatever status your OrderStatus enum supports
                payment.Order.UpdatedAt = DateTime.UtcNow; // If Order has UpdatedAt
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                status = "PAID",
                message = "Payment verified successfully! Order updated."
            });
        }

        return Ok(new
        {
            status = "PENDING",
            message = "Transaction has not been completed or found yet."
        });
    }

    // Helper method to generate Base64 QR Image using QRCoder
    private static string GenerateQrBase64(string qrString)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        byte[] qrCodeBytes = qrCode.GetGraphic(20); // 20 pixels per module size
        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }
}