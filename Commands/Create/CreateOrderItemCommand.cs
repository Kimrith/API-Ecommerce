using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Create
{
    // 1. COMMAND DEFINITION
    public record CreateOrderItemCommand(
        long OrderId,
        long ProductId,
        long? VariantId,
        int Quantity
    ) : IRequest<OrderDtos.Response>;

    // 2. COMMAND HANDLER
    public class CreateOrderItemCommandHandler : IRequestHandler<CreateOrderItemCommand, OrderDtos.Response>
    {
        private readonly AppDbContext _context;

        public CreateOrderItemCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDtos.Response> Handle(CreateOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (request.Quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.");
            }

            // 1. Fetch the Order
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.ShippingAddress)
                .Include(o => o.BillingAddress)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {request.OrderId} was not found.");
            }

            // Guard: Prevent modifying orders that are already shipped, delivered, or cancelled
            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
            {
                throw new InvalidOperationException($"Cannot add items to an order with status '{order.Status}'.");
            }

            // 2. Fetch the Product
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.ProductId} was not found.");
            }

            // 3. Determine Price from Main Product
            decimal unitPrice = (product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0)
                ? product.DiscountPrice.Value
                : product.Price;

            string? variantName = null;
            string? sku = null;

            if (request.VariantId.HasValue)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == request.VariantId.Value && v.ProductId == request.ProductId, cancellationToken);

                if (variant == null)
                {
                    throw new KeyNotFoundException($"Variant with ID {request.VariantId} was not found for this product.");
                }

                variantName = variant.Title;
                sku = variant.Sku;
                // Variant price block removed since pricing lives on the main Product
            }

            // 4. Create OrderItem Snapshot
            var newOrderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                VariantId = request.VariantId,
                ProductName = product.Name,
                VariantName = variantName,
                Sku = sku,
                Quantity = request.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * request.Quantity,
                CreatedAt = DateTime.UtcNow
            };

            _context.OrderItems.Add(newOrderItem);
            order.OrderItems.Add(newOrderItem);

            // 5. Recalculate Order Totals
            order.Subtotal = order.OrderItems.Sum(i => i.TotalPrice);
            order.TotalAmount = order.Subtotal + order.TaxAmount + order.ShippingAmount - order.DiscountAmount;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 6. Map and return updated Order
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