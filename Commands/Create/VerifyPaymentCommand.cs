using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using API_Ecommerce.Services;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Update
{
    public class VerifyPaymentCommand
    {
        public long OrderId { get; set; }
    }

    public class VerifyPaymentCommandHandler
    {
        private readonly AppDbContext _context;
        private readonly IBakongService _bakongService;

        public VerifyPaymentCommandHandler(AppDbContext context, IBakongService bakongService)
        {
            _context = context;
            _bakongService = bakongService;
        }

        public async Task<(bool Success, string Status, string Message)> HandleAsync(VerifyPaymentCommand command)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.OrderId == command.OrderId);

            if (payment == null)
            {
                return (false, "NOT_FOUND", "Payment record not found for this order.");
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                return (true, "PAID", "Payment is already completed.");
            }

            bool isPaid = await _bakongService.VerifyTransactionAsync(payment.Md5);

            if (isPaid)
            {
                payment.Status = PaymentStatus.Completed;

                if (payment.Order != null)
                {
                    payment.Order.Status = OrderStatus.Processing;
                    payment.Order.UpdatedAt = DateTime.UtcNow;

                    var orderItems = await _context.OrderItems
                        .Where(oi => oi.OrderId == payment.OrderId)
                        .ToListAsync();

                    foreach (var item in orderItems)
                    {
                        var inventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

                        if (inventory != null)
                        {
                            inventory.Quantity -= item.Quantity;
                            if (inventory.Quantity < 0)
                            {
                                inventory.Quantity = 0;
                            }
                            inventory.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return (true, "PAID", "Payment verified successfully! Order updated and stock decreased.");
            }

            return (true, "PENDING", "Transaction has not been completed or found yet.");
        }
    }
}