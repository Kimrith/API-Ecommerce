using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Delete
{
    // 1. COMMAND DEFINITION
    public record DeleteOrderItemCommand(
        long OrderId,
        long OrderItemId
    ) : IRequest<OrderDtos.Response>;

    // 2. COMMAND HANDLER
    public class DeleteOrderItemCommandHandler : IRequestHandler<DeleteOrderItemCommand, OrderDtos.Response>
    {
        private readonly AppDbContext _context;

        public DeleteOrderItemCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDtos.Response> Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
        {
            // 1. Fetch order with items
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.ShippingAddress)
                .Include(o => o.BillingAddress)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {request.OrderId} was not found.");
            }

            // Guard: Prevent modifying shipped or delivered orders
            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
            {
                throw new InvalidOperationException($"Cannot delete items from an order with status '{order.Status}'.");
            }

            // 2. Find target order item
            var itemToDelete = order.OrderItems.FirstOrDefault(i => i.Id == request.OrderItemId);
            if (itemToDelete == null)
            {
                throw new KeyNotFoundException($"Order item with ID {request.OrderItemId} was not found in Order #{request.OrderId}.");
            }

            // 3. Remove item
            _context.OrderItems.Remove(itemToDelete);
            order.OrderItems.Remove(itemToDelete);

            // 4. Recalculate Order Totals
            order.Subtotal = order.OrderItems.Sum(i => i.TotalPrice);
            order.TotalAmount = order.Subtotal + order.TaxAmount + order.ShippingAmount - order.DiscountAmount;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 5. Map and return updated order response
            return new OrderDtos.Response
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId ?? 0,
                Status = order.Status,
                Subtotal = order.Subtotal,
                TaxAmount = order.TaxAmount,
                ShippingAmount = order.ShippingAmount,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.OrderItems.Select(i => new OrderItemDtos.Response
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    VariantId = i.VariantId,
                    VariantName = i.VariantName,
                    Sku = i.Sku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice,
                    CreatedAt = i.CreatedAt
                }).ToList()
            };
        }
    }
}