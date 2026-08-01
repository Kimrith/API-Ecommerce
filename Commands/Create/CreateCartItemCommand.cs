using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CartModel = API_Ecommerce.Models.Cart;

namespace API_Ecommerce.Commands.Create
{
    // 1. COMMAND DEFINITION
    public record CreateCartItemCommand(
        long? UserId,
        string? SessionId,
        CartItemDtos.Create CartItemDto
    ) : IRequest<CartDtos.Response>;

    // 2. COMMAND HANDLER
    public class CreateCartItemCommandHandler : IRequestHandler<CreateCartItemCommand, CartDtos.Response>
    {
        private readonly AppDbContext _context;

        public CreateCartItemCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartDtos.Response> Handle(CreateCartItemCommand request, CancellationToken cancellationToken)
        {
            // Validate identity inputs
            if (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.SessionId))
            {
                throw new ArgumentException("Either UserId or SessionId must be provided to add an item to the cart.");
            }

            // 1. Validate Product & Price from Database
            var product = await _context.Products.FindAsync(new object[] { request.CartItemDto.ProductId }, cancellationToken);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.CartItemDto.ProductId} was not found.");
            }

            string? variantTitle = null;
            if (request.CartItemDto.VariantId.HasValue)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == request.CartItemDto.VariantId.Value, cancellationToken);

                if (variant == null)
                {
                    throw new KeyNotFoundException($"Variant with ID {request.CartItemDto.VariantId.Value} was not found.");
                }

                variantTitle = variant.Title;
            }

            // 2. Fetch existing cart (Prioritize UserId, fallback to SessionId)
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

            // 3. Lazy-Create Cart if missing
            if (cart == null)
            {
                cart = new CartModel
                {
                    UserId = request.UserId,
                    SessionId = request.UserId.HasValue ? null : request.SessionId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cart);
            }
            else
            {
                if (request.UserId.HasValue && !cart.UserId.HasValue)
                {
                    cart.UserId = request.UserId;
                    cart.SessionId = null;
                }

                cart.UpdatedAt = DateTime.UtcNow;
            }

            // 4. Upsert logic using cart.CartItems
            var existingItem = cart.CartItems.FirstOrDefault(i =>
                i.ProductId == request.CartItemDto.ProductId &&
                i.VariantId == request.CartItemDto.VariantId);

            if (existingItem != null)
            {
                existingItem.Quantity = Math.Min(99, existingItem.Quantity + request.CartItemDto.Quantity);
            }
            else
            {
                var newItem = new CartItem
                {
                    Cart = cart,
                    ProductId = request.CartItemDto.ProductId,
                    VariantId = request.CartItemDto.VariantId,
                    Quantity = request.CartItemDto.Quantity,
                    Price = product.Price
                };

                cart.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 5. Query full cart with relationships to map to DTOs
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
                    ProductName = i.Product?.Name ?? product.Name,
                    ProductImageUrl = i.Product?.ImageUrl ?? product.ImageUrl,
                    VariantId = i.VariantId,
                    VariantName = i.Variant?.Title ?? variantTitle,
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