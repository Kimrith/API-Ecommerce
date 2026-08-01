using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Cart
{
    public record ApplyCouponToCartCommand(
        long? UserId,
        string? SessionId,
        string CouponCode
    ) : IRequest<CartDtos.Response>;

    public class ApplyCouponToCartCommandHandler : IRequestHandler<ApplyCouponToCartCommand, CartDtos.Response>
    {
        private readonly AppDbContext _context;

        public ApplyCouponToCartCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartDtos.Response> Handle(ApplyCouponToCartCommand request, CancellationToken cancellationToken)
        {
            var cartQuery = _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Variant)
                .AsQueryable();

            // Explicitly use fully qualified model name to prevent namespace collision
            API_Ecommerce.Models.Cart? cart = null;

            if (request.UserId.HasValue)
            {
                cart = await cartQuery.FirstOrDefaultAsync(c => c.UserId == request.UserId.Value, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.SessionId))
            {
                cart = await cartQuery.FirstOrDefaultAsync(c => c.SessionId == request.SessionId, cancellationToken);
            }

            if (cart == null || !cart.CartItems.Any())
            {
                throw new KeyNotFoundException("Active shopping cart not found or cart is empty.");
            }

            var cleanCode = request.CouponCode.Trim();
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == cleanCode.ToLower(), cancellationToken);

            if (coupon == null || !coupon.IsActive)
            {
                throw new InvalidOperationException("Invalid or inactive coupon code.");
            }

            var now = DateTime.UtcNow;
            if ((coupon.StartsAt.HasValue && now < coupon.StartsAt.Value) ||
                (coupon.ExpiresAt.HasValue && now > coupon.ExpiresAt.Value))
            {
                throw new InvalidOperationException("This coupon has expired or is not yet active.");
            }

            if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
            {
                throw new InvalidOperationException("This coupon has reached its total usage limit.");
            }

            decimal subtotal = cart.CartItems.Sum(i => i.Price * i.Quantity);

            if (coupon.MinimumAmount.HasValue && subtotal < coupon.MinimumAmount.Value)
            {
                throw new InvalidOperationException($"Cart subtotal must be at least {coupon.MinimumAmount.Value:C} to use this coupon.");
            }

            decimal discountAmount = 0;
            if (coupon.DiscountType == CouponType.Percentage)
            {
                discountAmount = subtotal * (coupon.DiscountValue / 100m);
                if (coupon.MaximumDiscountAmount.HasValue && discountAmount > coupon.MaximumDiscountAmount.Value)
                {
                    discountAmount = coupon.MaximumDiscountAmount.Value;
                }
            }
            else
            {
                discountAmount = coupon.DiscountValue;
                if (discountAmount > subtotal)
                {
                    discountAmount = subtotal;
                }
            }

            cart.AppliedCouponCode = coupon.Code;
            cart.DiscountAmount = discountAmount;
            cart.TotalAmount = Math.Max(0, subtotal - discountAmount);
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new CartDtos.Response
            {
                Id = cart.Id,
                UserId = cart.UserId,
                SessionId = cart.SessionId,
                Items = cart.CartItems.Select(i => new CartItemDtos.Response
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    ProductImageUrl = i.Product?.ImageUrl,
                    VariantId = i.VariantId,
                    // If your ProductVariants model uses a different property name for name, change it here
                    VariantName = null,
                    Quantity = i.Quantity,
                    Price = i.Price
                    // Removed Subtotal assignment since it is a calculated read-only property in the DTO
                }).ToList(),
                SubtotalAmount = subtotal,
                AppliedCouponCode = cart.AppliedCouponCode,
                DiscountAmount = cart.DiscountAmount,
                TotalAmount = cart.TotalAmount,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                ExpiresAt = cart.ExpiresAt
            };
        }
    }
}