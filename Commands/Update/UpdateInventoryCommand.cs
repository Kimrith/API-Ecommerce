using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Update
{
    public class UpdateInventoryCommand : IRequest<InventoryResponseDto?>
    {
        public long Id { get; set; }
        public UpdateInventoryDto InventoryDto { get; set; }

        public UpdateInventoryCommand(long id, UpdateInventoryDto inventoryDto)
        {
            Id = id;
            InventoryDto = inventoryDto;
        }
    }

    public class UpdateInventoryCommandHandler : IRequestHandler<UpdateInventoryCommand, InventoryResponseDto?>
    {
        private readonly AppDbContext _context;

        public UpdateInventoryCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryResponseDto?> Handle(UpdateInventoryCommand request, CancellationToken cancellationToken)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Variant)
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (inventory == null)
            {
                return null;
            }

            var dto = request.InventoryDto;

            inventory.Quantity = dto.Quantity;
            inventory.ReservedQuantity = dto.ReservedQuantity;
            inventory.ReorderLevel = dto.ReorderLevel;
            inventory.ReorderQuantity = dto.ReorderQuantity;
            inventory.WarehouseLocation = dto.WarehouseLocation;
            inventory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new InventoryResponseDto
            {
                Id = inventory.Id,
                ProductId = inventory.ProductId,
                ProductName = inventory.Product?.Name,
                VariantId = inventory.VariantId,
                VariantName = inventory.Variant?.Title,
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