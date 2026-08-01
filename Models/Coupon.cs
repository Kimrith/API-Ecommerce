using API_Ecommerce.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("coupons")]
    public class Coupon
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        public CouponType DiscountType { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscountAmount { get; set; }

        public int? UsageLimit { get; set; }

        public int? UsageLimitPerUser { get; set; }

        public int TimesUsed { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime? StartsAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for tracking usage history
        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
    }
}