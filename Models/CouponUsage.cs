using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("coupon_usages")]
    public class CouponUsage
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long CouponId { get; set; }

        [ForeignKey(nameof(CouponId))]
        public virtual Coupon? Coupon { get; set; }

        [Required]
        public long UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual Auth? User { get; set; }

        [Required]
        public long OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}