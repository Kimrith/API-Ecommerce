using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CartModel = API_Ecommerce.Models.Cart;

namespace API_Ecommerce.Commands.Delete
{
    // 1. COMMAND DEFINITION
    public record DeleteCartItemCommand(
        long? UserId,
        string? SessionId,
        long CartItemId
    ) : IRequest<CartDtos.Response>;

    // 2. COMMAND HANDLER
    public class DeleteCartItemCommandHandler : IRequestHandler<DeleteCartItemCommand, CartDtos.Response>
    {
        private readonly AppDbContext _context;

        public DeleteCartItemCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartDtos.Response> Handle(DeleteCartItemCommand request, CancellationToken cancellationToken)
        {
            // Validate identity inputs
            if (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.SessionId))
            {
                throw new ArgumentException("Either UserId or SessionId must be provided to remove an item from the cart.");
            }

            // 1. Fetch existing cart (Prioritize UserId, fallback to SessionId)
            CartModel? cart = null;

            if (request.UserId.HasValue)
            {
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == request.UserId.Value, cancellationToken);
            }

            if (cart == null && !string.IsNullOrWhiteSpace(request.SessionId))
            {
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.SessionId == request.SessionId, cancellationToken);
            }

            if (cart == null)
            {
                throw new KeyNotFoundException("Cart not found.");
            }

            // 2. Find the cart item to delete
            var cartItem = cart.CartItems.FirstOrDefault(i => i.Id == request.CartItemId);
            if (cartItem == null)
            {
                throw new KeyNotFoundException($"Cart item with ID {request.CartItemId} was not found in the cart.");
            }

            // 3. Remove item from database/context & update cart timestamp
            _context.CartItems.Remove(cartItem);
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Query full updated cart with relationships to return fresh response DTO
            var updatedCart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Variant)
                .FirstAsync(c => c.Id == cart.Id, cancellationToken);

            decimal subtotal = updatedCart.CartItems.Sum(i => i.Quantity * i.Price);

            return new CartDtos.Response
            {
                Id = updatedCart.Id,
                UserId = updatedCart.UserId,
                SessionId = updatedCart.SessionId,
                CreatedAt = updatedCart.CreatedAt,
                UpdatedAt = updatedCart.UpdatedAt,
                ExpiresAt = updatedCart.ExpiresAt,
                Items = updatedCart.CartItems.Select(i => new CartItemDtos.Response
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    ProductImageUrl = i.Product?.ImageUrl,
                    VariantId = i.VariantId,
                    VariantName = i.Variant?.Title,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList(),
                SubtotalAmount = subtotal,
                AppliedCouponCode = updatedCart.AppliedCouponCode,
                DiscountAmount = updatedCart.DiscountAmount,
                TotalAmount = updatedCart.TotalAmount > 0 ? updatedCart.TotalAmount : subtotal
            };
        }
    }
}