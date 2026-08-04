using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Queries
{
    // ==========================================
    // 1. Get All Inventory Query
    // ==========================================
    public class GetAllInventoryQuery : IRequest<IEnumerable<InventoryResponseDto>>
    {
    }

    public class GetAllInventoryQueryHandler : IRequestHandler<GetAllInventoryQuery, IEnumerable<InventoryResponseDto>>
    {
        private readonly AppDbContext _context;

        public GetAllInventoryQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InventoryResponseDto>> Handle(GetAllInventoryQuery request, CancellationToken cancellationToken)
        {
            var inventories = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Variant)
                .ToListAsync(cancellationToken);

            return inventories.Select(inventory => new InventoryResponseDto
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
            });
        }
    }

    // ==========================================
    // 2. Get Inventory By Id Query
    // ==========================================
    public class GetInventoryByIdQuery : IRequest<InventoryResponseDto?>
    {
        public long Id { get; set; }

        public GetInventoryByIdQuery(long id)
        {
            Id = id;
        }
    }

    public class GetInventoryByIdQueryHandler : IRequestHandler<GetInventoryByIdQuery, InventoryResponseDto?>
    {
        private readonly AppDbContext _context;

        public GetInventoryByIdQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryResponseDto?> Handle(GetInventoryByIdQuery request, CancellationToken cancellationToken)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Variant)
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (inventory == null)
            {
                return null;
            }

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