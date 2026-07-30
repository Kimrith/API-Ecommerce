using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    [Table("coupons")]
    public class Coupon
    {
        [Key]
        public long Id { get; set; }

        // Unique promotional code entered at checkout (e.g., "SUMMER20")
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        // Discount calculation strategy
        [Required]
        public CouponType DiscountType { get; set; } = CouponType.Percentage;

        // Discount value: e.g., 20 for 20% OR 15.00 for $15 off
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        // --- Usage Restrictions ---
        // Minimum order subtotal required to use this coupon (e.g., $50)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumAmount { get; set; }

        // Maximum discount amount allowed for percentage discounts (e.g., cap 20% off at $50 max)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscountAmount { get; set; }

        // Global usage cap (e.g., first 100 customers only)
        public int? UsageLimit { get; set; }

        // Per-user usage cap (e.g., max 1 use per customer)
        public int? UsageLimitPerUser { get; set; }

        // Current count of how many times this coupon has been redeemed
        public int TimesUsed { get; set; } = 0;

        // Active status flag for instant enable/disable by admins
        public bool IsActive { get; set; } = true;

        // --- Validity Period ---
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        // --- Navigation Properties ---
        public virtual ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}