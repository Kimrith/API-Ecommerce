using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Update
{
    // 1. COMMAND DEFINITION
    public record UpdateCartItemCommand(
        long? UserId,
        string? SessionId,
        long CartItemId,
        CartItemDtos.UpdateQuantity UpdateDto
    ) : IRequest<CartDtos.Response>;

    // 2. COMMAND HANDLER
    public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, CartDtos.Response>
    {
        private readonly AppDbContext _context;

        public UpdateCartItemCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartDtos.Response> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
        {
            // Validate identity inputs
            if (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.SessionId))
            {
                throw new ArgumentException("Either UserId or SessionId must be provided to update a cart item.");
            }

            // 1. Fetch existing cart (Prioritize UserId, fallback to SessionId)
            Cart? cart = null;

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

            // 2. Find the target cart item
            var cartItem = cart.CartItems.FirstOrDefault(i => i.Id == request.CartItemId);
            if (cartItem == null)
            {
                throw new KeyNotFoundException($"Cart item with ID {request.CartItemId} was not found in the cart.");
            }

            // 3. Update item quantity and cart timestamp
            cartItem.Quantity = request.UpdateDto.Quantity;
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Query full updated cart with relationships to return fresh response DTO
            var updatedCart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Variant)
                .FirstAsync(c => c.Id == cart.Id, cancellationToken);

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
                TotalAmount = updatedCart.CartItems.Sum(i => i.Quantity * i.Price)
            };
        }
    }
}