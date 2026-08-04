using System.ComponentModel.DataAnnotations;

namespace API_Ecommerce.DTOs
{
    // ==========================================
    // 1. Response DTO (For reading inventory data)
    // ==========================================
    public class InventoryResponseDto
    {
        public long Id { get; set; }
        public long? ProductId { get; set; }
        public string? ProductName { get; set; } // Optional: convenient if you want to include parent product name
        public long? VariantId { get; set; }
        public string? VariantName { get; set; } // Optional: convenient if you want to include variant details
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public string? WarehouseLocation { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ==========================================
    // 2. Create DTO (For adding new inventory)
    // ==========================================
    public class CreateInventoryDto
    {
        public long? ProductId { get; set; }
        public long? VariantId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0.")]
        public int Quantity { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Reorder level must be 0 or greater.")]
        public int ReorderLevel { get; set; } = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Reorder quantity must be at least 1.")]
        public int ReorderQuantity { get; set; } = 50;

        [StringLength(50)]
        public string? WarehouseLocation { get; set; }
    }

    // ==========================================
    // 3. Update DTO (For modifying inventory)
    // ==========================================
    public class UpdateInventoryDto
    {
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0.")]
        public int Quantity { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Reserved quantity must be greater than or equal to 0.")]
        public int ReservedQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Reorder level must be 0 or greater.")]
        public int ReorderLevel { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Reorder quantity must be at least 1.")]
        public int ReorderQuantity { get; set; }

        [StringLength(50)]
        public string? WarehouseLocation { get; set; }
    }
}