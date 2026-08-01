using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("cart_items")]
    public class CartItem
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long CartId { get; set; }

        [ForeignKey(nameof(CartId))]
        public virtual Cart? Cart { get; set; }

        [Required]
        public long ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        public long? VariantId { get; set; }

        [ForeignKey(nameof(VariantId))]
        public virtual ProductVariants? Variant { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}