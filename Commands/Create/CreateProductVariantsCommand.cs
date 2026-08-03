using MediatR;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using API_Ecommerce.Data;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Create
{
    // --- Request Command ---
    public record CreateProductVariantCommand(CreateProductVariantDto Dto) : IRequest<ProductVariantResponseDto?>;

    // --- Command Handler ---
    public class CreateProductVariantCommandHandler : IRequestHandler<CreateProductVariantCommand, ProductVariantResponseDto?>
    {
        private readonly AppDbContext _context;

        public CreateProductVariantCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProductVariantResponseDto?> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // 1. Verify that the parent product exists
            var parentProductExists = await _context.Products
                .AnyAsync(p => p.Id == dto.ProductId, cancellationToken);

            if (!parentProductExists)
            {
                return null; // Or throw a NotFoundException depending on your error handling setup
            }

            // 2. Map DTO to Entity (StockQuantity removed from ProductVariants)
            var variant = new ProductVariants
            {
                ProductId = dto.ProductId,
                Title = dto.Title,
                Sku = dto.Sku,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                ImageUrl = dto.ImageUrl,
                Size = dto.Size,
                Color = dto.Color,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            // 3. Save variant to database to generate ID
            await _context.ProductVariants.AddAsync(variant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Create the Inventory record using InitialStock
            var inventory = new Inventory
            {
                ProductId = null, // Parent is variant-backed
                VariantId = variant.Id,
                Quantity = dto.InitialStock,
                ReservedQuantity = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Map Entity & Inventory to Response DTO
            return new ProductVariantResponseDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                Title = variant.Title,
                Sku = variant.Sku,
                Price = variant.Price,
                DiscountPrice = variant.DiscountPrice,
                StockQuantity = inventory.Quantity,
                AvailableQuantity = inventory.AvailableQuantity,
                ImageUrl = variant.ImageUrl,
                Size = variant.Size,
                Color = variant.Color,
                IsActive = variant.IsActive,
                CreatedAt = variant.CreatedAt,
                UpdatedAt = variant.UpdatedAt
            };
        }
    }
}