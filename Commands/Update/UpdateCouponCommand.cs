using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace API_Ecommerce.Commands.Update
{
    // =========================================================
    // 1. COMMAND RECORD
    // =========================================================
    public record UpdateCouponCommand(
        long Id,
        [Required][StringLength(50)] string Code,
        string? Description,
        CouponType DiscountType,
        [Range(0.01, double.MaxValue)] decimal DiscountValue,
        decimal? MinimumAmount,
        decimal? MaximumDiscountAmount,
        int? UsageLimit,
        int? UsageLimitPerUser,
        DateTime? StartsAt,
        DateTime? ExpiresAt,
        bool IsActive
    ) : IRequest<Coupon>;

    // =========================================================
    // 2. COMMAND HANDLER
    // =========================================================
    public class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, Coupon>
    {
        private readonly AppDbContext _context;

        public UpdateCouponCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Coupon> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (coupon == null)
            {
                throw new KeyNotFoundException($"Coupon with ID {request.Id} was not found.");
            }

            // Check if the updated code already exists on another coupon
            bool codeExists = await _context.Coupons
                .AnyAsync(c => c.Code.ToLower() == request.Code.ToLower() && c.Id != request.Id, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Coupon code '{request.Code}' is already taken by another coupon.");
            }

            // Update properties
            coupon.Code = request.Code.Trim().ToUpper();
            coupon.Description = request.Description;
            coupon.DiscountType = request.DiscountType;
            coupon.DiscountValue = request.DiscountValue;
            coupon.MinimumAmount = request.MinimumAmount;
            coupon.MaximumDiscountAmount = request.MaximumDiscountAmount;
            coupon.UsageLimit = request.UsageLimit;
            coupon.UsageLimitPerUser = request.UsageLimitPerUser;
            coupon.StartsAt = request.StartsAt;
            coupon.ExpiresAt = request.ExpiresAt;
            coupon.IsActive = request.IsActive;
            coupon.UpdatedAt = DateTime.UtcNow;

            _context.Coupons.Update(coupon);
            await _context.SaveChangesAsync(cancellationToken);

            return coupon;
        }
    }
}