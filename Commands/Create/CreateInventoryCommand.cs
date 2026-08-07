using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using MediatR;

namespace API_Ecommerce.Commands.Create
{
    public class CreateInventoryCommand : IRequest<InventoryResponseDto>
    {
        public CreateInventoryDto InventoryDto { get; set; }

        public CreateInventoryCommand(CreateInventoryDto inventoryDto)
        {
            InventoryDto = inventoryDto;
        }
    }

    public class CreateInventoryCommandHandler : IRequestHandler<CreateInventoryCommand, InventoryResponseDto>
    {
        private readonly AppDbContext _context; // Replace with your actual DbContext name

        public CreateInventoryCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryResponseDto> Handle(CreateInventoryCommand request, CancellationToken cancellationToken)
        {
            var dto = request.InventoryDto;

            var inventory = new Inventory
            {
                ProductId = dto.ProductId,
                VariantId = dto.VariantId,
                Quantity = dto.Quantity,
                ReservedQuantity = 0, // New inventory starts with 0 reserved
                ReorderLevel = dto.ReorderLevel,
                ReorderQuantity = dto.ReorderQuantity,
                WarehouseLocation = dto.WarehouseLocation,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync(cancellationToken);

            // Optional: Load navigation properties if needed for the response
            // string? productName = ...;
            // string? variantName = ...;

            return new InventoryResponseDto
            {
                Id = inventory.Id,
                ProductId = inventory.ProductId,
                ProductName = null, // Populate if you fetch product data
                VariantId = inventory.VariantId,
                VariantName = null, // Populate if you fetch variant data
                Quantity = inventory.Quantity,
                ReservedQuantity = inventory.ReservedQuantity,
                AvailableQuantity = inventory.AvailableQuantity,
                ReorderLevel = inventory.ReorderLevel,
                ReorderQuantity = inventory.ReorderQuantity,
                WarehouseLocation = inventory.WarehouseLocation,
                UpdatedAt = inventory.UpdatedAt
            };
        }
    }
}