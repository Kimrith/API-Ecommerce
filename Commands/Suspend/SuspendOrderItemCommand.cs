using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Suspend
{
    // 1. COMMAND DEFINITION
    public record SuspendOrderCommand(
        long OrderId,
        string Reason
    ) : IRequest<OrderDtos.Response>;

    // 2. COMMAND HANDLER
    public class SuspendOrderCommandHandler : IRequestHandler<SuspendOrderCommand, OrderDtos.Response>
    {
        private readonly AppDbContext _context;

        public SuspendOrderCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDtos.Response> Handle(SuspendOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.ShippingAddress)
                .Include(o => o.BillingAddress)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {request.OrderId} was not found.");
            }

            // Guard check: cannot suspend already delivered or cancelled orders
            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
            {
                throw new InvalidOperationException($"Cannot suspend an order with status {order.Status}.");
            }

            // Update status to Cancelled / Suspended
            order.Status = OrderStatus.Cancelled; // Or OrderStatus.Suspended if added to enum
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? $"[Suspended]: {request.Reason}"
                : $"{order.Notes} | [Suspended]: {request.Reason}";

            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

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