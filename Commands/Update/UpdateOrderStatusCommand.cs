using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Update
{
    public class UpdateOrderStatusCommandHandler
    {
        private readonly AppDbContext _context;

        public UpdateOrderStatusCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, object Data)> HandleAsync(long id, OrderDtos.UpdateStatus dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return (false, $"Order with ID {id} was not found.", null);
            }

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var responseData = new
            {
                message = "Order status updated successfully.",
                orderId = order.Id,
                status = order.Status.ToString()
            };

            return (true, "Success", responseData);
        }
    }
}