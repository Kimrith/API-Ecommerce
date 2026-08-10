using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Cart
{
    // 1. Updated Request Command accepting items from Angular local storage
    public record ApplyCouponToCartCommand(
        long? UserId,
        string CouponCode,
        List<CartItemDto> CartItems
    ) : IRequest<CartCalculationResponseDto>;

    public class ApplyCouponToCartCommandHandler : IRequestHandler<ApplyCouponToCartCommand, CartCalculationResponseDto>
    {
        private readonly AppDbContext _context;

        public ApplyCouponToCartCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartCalculationResponseDto> Handle(ApplyCouponToCartCommand request, CancellationToken cancellationToken)
        {
            if (request.CartItems == null || !request.CartItems.Any())
            {
                throw new InvalidOperationException("Cart is empty.");
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

            // Optional: Check per-user limit if UserId is provided
            if (request.UserId.HasValue && coupon.UsageLimitPerUser.HasValue)
            {
                int userUsageCount = await _context.CouponUsages
                    .CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == request.UserId.Value, cancellationToken);

                if (userUsageCount >= coupon.UsageLimitPerUser.Value)
                {
                    throw new InvalidOperationException("You have already reached your usage limit for this coupon.");
                }
            }

            // Calculate subtotal from items coming from Angular local storage
            decimal subtotal = request.CartItems.Sum(i => i.Price * i.Quantity);

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

            decimal totalAmount = Math.Max(0, subtotal - discountAmount);

            // Return calculated results back to Angular so it can update state
            return new CartCalculationResponseDto
            {
                AppliedCouponCode = coupon.Code,
                SubtotalAmount = subtotal,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount
            };
        }
    }

    // Response DTO for frontend calculation update
    public class CartCalculationResponseDto
    {
        public string AppliedCouponCode { get; set; } = string.Empty;
        public decimal SubtotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // Item structure matching localStorage data sent from Angular
    public class CartItemDto
    {
        public long ProductId { get; set; }
        public long? VariantId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}