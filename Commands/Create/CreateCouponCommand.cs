using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace API_Ecommerce.Commands.Create
{
    // =========================================================
    // 1. COMMAND RECORD
    // =========================================================
    public record CreateCouponCommand(
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
        bool IsActive = true
    ) : IRequest<Coupon>; // Returns the created Coupon entity or a Coupon DTO

    // =========================================================
    // 2. COMMAND HANDLER
    // =========================================================
    public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Coupon>
    {
        private readonly AppDbContext _context;

        public CreateCouponCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Coupon> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
        {
            // Check if coupon code already exists (case-insensitive check usually preferred)
            bool codeExists = await _context.Coupons
                .AnyAsync(c => c.Code.ToLower() == request.Code.ToLower(), cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Coupon code '{request.Code}' already exists.");
            }

            // Map command to your Coupon entity
            var coupon = new Coupon
            {
                Code = request.Code.Trim().ToUpper(),
                Description = request.Description,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                MinimumAmount = request.MinimumAmount,
                MaximumDiscountAmount = request.MaximumDiscountAmount,
                UsageLimit = request.UsageLimit,
                UsageLimitPerUser = request.UsageLimitPerUser,
                StartsAt = request.StartsAt,
                ExpiresAt = request.ExpiresAt,
                IsActive = request.IsActive,
                TimesUsed = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync(cancellationToken);

            return coupon;
        }
    }
}