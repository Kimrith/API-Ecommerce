using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using API_Ecommerce.Services;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using API_Ecommerce.DTOs;

namespace API_Ecommerce.Commands.Create
{
    public class CreatePaymentCommand
    {
        public int UserId { get; set; }
        public List<CheckoutCartItemDto> Items { get; set; } = new();
    }

    public class CreatePaymentCommandHandler
    {
        private readonly AppDbContext _context;
        private readonly IBakongService _bakongService;

        public CreatePaymentCommandHandler(AppDbContext context, IBakongService bakongService)
        {
            _context = context;
            _bakongService = bakongService;
        }

        public async Task<(bool Success, string Message, object Data)> HandleAsync(CreatePaymentCommand command)
        {
            if (command?.Items == null || !command.Items.Any())
            {
                return (false, "Cart is empty or not found.", null);
            }

            int userId = command.UserId > 0 ? command.UserId : 8;
            decimal subtotal = 0;
            int validItemsCount = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var cartItem in command.Items)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product == null) continue;

                    decimal unitPrice = cartItem.Price > 0 ? cartItem.Price : product.Price;
                    subtotal += unitPrice * cartItem.Quantity;
                    validItemsCount++;
                }

                if (validItemsCount == 0)
                {
                    await transaction.RollbackAsync();
                    return (false, "No valid products found in cart.", null);
                }

                decimal taxAmount = 0;
                decimal shippingAmount = 0;
                decimal discountAmount = 0;
                decimal totalAmount = subtotal + taxAmount + shippingAmount - discountAmount;

                string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = orderNumber,
                    Subtotal = subtotal,
                    TaxAmount = taxAmount,
                    ShippingAmount = shippingAmount,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    Currency = "USD",
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var cartItem in command.Items)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product == null) continue;

                    decimal unitPrice = cartItem.Price > 0 ? cartItem.Price : product.Price;
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * cartItem.Quantity
                    };
                    _context.OrderItems.Add(orderItem);
                }
                await _context.SaveChangesAsync();

                string orderReference = order.OrderNumber;
                var (qrString, md5) = _bakongService.GenerateDynamicQr(orderReference, order.TotalAmount, "USD");

                if (string.IsNullOrEmpty(qrString) || string.IsNullOrEmpty(md5))
                {
                    await transaction.RollbackAsync();
                    return (false, "Failed to generate Bakong KHQR.", null);
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

                string qrImageBase64 = GenerateQrBase64(qrString);

                var resultData = new
                {
                    orderId = order.Id,
                    orderReference = orderReference,
                    amount = order.TotalAmount,
                    khqrString = qrString,
                    qrImageBase64 = qrImageBase64,
                    md5 = md5
                };

                return (true, "QR generated and order processed successfully.", resultData);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"An error occurred while processing the order: {ex.Message}", null);
            }
        }

        private static string GenerateQrBase64(string qrString)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }
    }
}