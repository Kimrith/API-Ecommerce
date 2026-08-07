using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_Ecommerce.Models
{
    [Table("inventory")]
    public class Inventory
    {
        [Key]
        public long Id { get; set; }

        // --- Foreign Key to Parent Product (Used if product has NO variants) ---
        public long? ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        // --- Foreign Key to Variant (Used if product HAS variants) ---
        public long? VariantId { get; set; }

        [ForeignKey(nameof(VariantId))]
        public virtual ProductVariants? Variant { get; set; }

        // --- Quantity Tracking ---
        [Required]
        public int Quantity { get; set; } = 0;

        [Required]
        public int ReservedQuantity { get; set; } = 0;

        // --- Reorder Metrics ---
        public int ReorderLevel { get; set; } = 10;

        public int ReorderQuantity { get; set; } = 50;

        // --- Warehouse / Logistics ---
        [StringLength(50)]
        public string? WarehouseLocation { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Calculated Helper Property ---
        [NotMapped]
        public int AvailableQuantity => Quantity - ReservedQuantity;
    }
}