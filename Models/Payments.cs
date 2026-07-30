using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using API_Ecommerce.Enums;

namespace API_Ecommerce.Models
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        public long Id { get; set; }

        // --- Order Relationship ---
        [Required]
        public long OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        // --- Payment Method ---
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "BakongKHQR";

        // --- Bakong Specific Fields ---
        // Bakong transaction hash returned after payment verification
        [StringLength(255)]
        public string? BakongHash { get; set; }

        // MD5 string generated for KHQR verification
        [StringLength(64)]
        public string? Md5 { get; set; }

        // Raw KHQR string generated for rendering QR code on frontend
        public string? KhqrString { get; set; }

        // Account ID of receiver/merchant (e.g. "your_merchant@acleda")
        [StringLength(100)]
        public string? ReceiverAccountId { get; set; }

        // Account ID of sender/customer (populated after transaction completes)
        [StringLength(100)]
        public string? SenderAccountId { get; set; }

        // --- Financials ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Bakong supports both "USD" and "KHR"
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        // --- Status & External Reference ---
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // External transaction reference / bill number
        [StringLength(100)]
        public string? ExternalTransactionId { get; set; }

        // Optional raw JSON payload response from Bakong API for debugging
        public string? RawResponse { get; set; }

        // --- Timestamps ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}