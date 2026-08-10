// ==========================================
// 1. UPDATED ORDER MODEL (Models/Order.cs)
// ==========================================
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        public long Id { get; set; }

        public long? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual Auth? User { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        public long? ShippingAddressId { get; set; }

        [ForeignKey(nameof(ShippingAddressId))]
        public virtual Address? ShippingAddress { get; set; }

        public long? BillingAddressId { get; set; }

        [ForeignKey(nameof(BillingAddressId))]
        public virtual Address? BillingAddress { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}