using System.ComponentModel.DataAnnotations;
using API_Ecommerce.Enums;

namespace API_Ecommerce.DTOs
{
    public class CouponDtos
    {
        // Response DTO returned to client/admin
        public class Response
        {
            public long Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string? Description { get; set; }
            public CouponType DiscountType { get; set; }
            public decimal DiscountValue { get; set; }
            public decimal? MinimumAmount { get; set; }
            public decimal? MaximumDiscountAmount { get; set; }
            public int? UsageLimit { get; set; }
            public int? UsageLimitPerUser { get; set; }
            public int TimesUsed { get; set; }
            public bool IsActive { get; set; }
            public DateTime? StartsAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        // Request DTO for Admin to create a coupon
        public class Create
        {
            [Required]
            [StringLength(50)]
            public string Code { get; set; } = string.Empty;

            [StringLength(255)]
            public string? Description { get; set; }

            [Required]
            public CouponType DiscountType { get; set; }

            [Required]
            [Range(0.01, 1000000)]
            public decimal DiscountValue { get; set; }

            public decimal? MinimumAmount { get; set; }
            public decimal? MaximumDiscountAmount { get; set; }
            public int? UsageLimit { get; set; }
            public int? UsageLimitPerUser { get; set; }
            public bool IsActive { get; set; } = true;
            public DateTime? StartsAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        // Request DTO for Admin to update a coupon
        public class Update : Create
        {
            // Inherits all fields from Create
        }

        // Request DTO for a user applying a coupon to their cart
        public class Apply
        {
            [Required]
            [StringLength(50)]
            public string Code { get; set; } = string.Empty;
        }
    }
}