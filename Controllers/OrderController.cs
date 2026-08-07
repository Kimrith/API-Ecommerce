using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API_Ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Order
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    CustomerName = o.User != null ? o.User.FullName : "Unknown",
                    CustomerEmail = o.User != null ? o.User.Email : "Unknown",
                    o.TotalAmount,
                    o.Status,
                    StatusString = o.Status.ToString(),
                    o.CreatedAt,
                    o.OrderNumber,
                    o.Currency,
                    o.Notes,
                    ShippingAddress = o.ShippingAddress != null ? new
                    {
                        o.ShippingAddress.Id,
                        o.ShippingAddress.StreetAddress,
                        o.ShippingAddress.City,
                        o.ShippingAddress.State,
                        o.ShippingAddress.PostalCode,
                        o.ShippingAddress.Country,
                        o.ShippingAddress.AddressType,
                        o.ShippingAddress.IsDefault
                    } : null
                })
                .ToListAsync();

            return Ok(orders);
        }

        // 2. GET: api/Order/statistics
        [HttpGet("statistics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderStatistics()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders
                .SumAsync(o => o.TotalAmount);

            var pendingCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            var processingCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing);
            var shippedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipped);
            var deliveredCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Refunded);
            var completedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
            var cancelledCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);

            // Monthly revenue analytics (last 6 months)
            var now = DateTime.UtcNow;
            var monthlyRevenue = new List<decimal>();
            var monthlyLabels = new List<string>();

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = now.AddMonths(-i);
                var label = targetDate.ToString("MMM");
                monthlyLabels.Add(label);

                var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                var sum = await _context.Orders
                    .Where(o => o.CreatedAt >= startOfMonth && o.CreatedAt <= endOfMonth)
                    .SumAsync(o => o.TotalAmount);

                monthlyRevenue.Add(sum);
            }

            return Ok(new
            {
                totalOrders,
                totalRevenue,
                pendingCount,
                processingCount,
                shippedCount,
                deliveredCount,
                completedCount,
                cancelledCount,
                analytics = new
                {
                    labels = monthlyLabels,
                    data = monthlyRevenue
                }
            });
        }

        // 3. PATCH: api/Order/{id}/status
        [HttpPatch("{id:long}/status")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateOrderStatus(long id, [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} was not found." });
            }

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order status updated successfully.",
                orderId = order.Id,
                status = order.Status.ToString()
            });
        }
    }

    public class UpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }
    }
}
