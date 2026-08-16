using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using API_Ecommerce.Services;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace API_Ecommerce.Queries
{
    public class GenerateOrderQrQuery
    {
        private readonly AppDbContext _context;
        private readonly IBakongService _bakongService;

        public GenerateOrderQrQuery(AppDbContext context, IBakongService bakongService)
        {
            _context = context;
            _bakongService = bakongService;
        }

        public async Task<OrderQrResponseDto> ExecuteAsync(long orderId)
        {
            // 1. Find the order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {orderId} was not found.");
            }

            // 2. Query OrderItems directly from the database table to avoid navigation property mismatches
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Seller)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            // 3. Check if a payment record already exists for this order with a valid KHQR string
            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
            if (existingPayment != null && !string.IsNullOrEmpty(existingPayment.KhqrString))
            {
                return new OrderQrResponseDto
                {
                    KhqrString = existingPayment.KhqrString,
                    Md5 = existingPayment.Md5,
                    QrImageBase64 = GenerateQrBase64(existingPayment.KhqrString)
                };
            }

            // 4. Determine Bakong configuration based on the first product's seller
            string? qrString = null;
            string? md5 = null;

            var firstItem = orderItems.FirstOrDefault();
            if (firstItem?.Product?.Seller != null)
            {
                var seller = firstItem.Product.Seller;
                if (seller.Role == Roles.Seller)
                {
                    var config = await _context.SellerBakongConfigs
                        .FirstOrDefaultAsync(c => c.SellerId == (int)seller.Id);

                    if (config != null && !string.IsNullOrEmpty(config.BakongId))
                    {
                        (qrString, md5) = _bakongService.GenerateDynamicQr(
                            order.OrderNumber,
                            order.TotalAmount,
                            config.BakongId,
                            config.MerchantName,
                            config.MerchantCity,
                            config.AcquiringId,
                            order.Currency
                        );
                    }
                    else
                    {
                        (qrString, md5) = _bakongService.GenerateDynamicQr(order.OrderNumber, order.TotalAmount, order.Currency);
                    }
                }
                else
                {
                    (qrString, md5) = _bakongService.GenerateDynamicQr(order.OrderNumber, order.TotalAmount, order.Currency);
                }
            }
            else
            {
                (qrString, md5) = _bakongService.GenerateDynamicQr(order.OrderNumber, order.TotalAmount, order.Currency);
            }

            // 5. Save or update the payment record in the database
            if (existingPayment != null)
            {
                existingPayment.KhqrString = qrString ?? string.Empty;
                existingPayment.Md5 = md5 ?? string.Empty;
                _context.Payments.Update(existingPayment);
            }
            else
            {
                var payment = new Payment
                {
                    OrderId = order.Id,
                    PaymentMethod = "BakongKHQR",
                    Amount = order.TotalAmount,
                    Currency = order.Currency,
                    KhqrString = qrString ?? string.Empty,
                    Md5 = md5 ?? string.Empty,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();

            // 6. Generate Base64 QR Image string and return response DTO
            string qrImageBase64 = !string.IsNullOrEmpty(qrString) ? GenerateQrBase64(qrString) : string.Empty;

            return new OrderQrResponseDto
            {
                KhqrString = qrString,
                Md5 = md5,
                QrImageBase64 = qrImageBase64
            };
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

    public class OrderQrResponseDto
    {
        public string? KhqrString { get; set; }
        public string? Md5 { get; set; }
        public string QrImageBase64 { get; set; } = string.Empty;
    }
}