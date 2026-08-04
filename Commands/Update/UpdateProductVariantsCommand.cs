using MediatR;
using API_Ecommerce.DTOs;
using API_Ecommerce.Data;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Update
{
    // --- Request Command ---
    public record UpdateProductVariantCommand(long Id, UpdateProductVariantDto Dto) : IRequest<ProductVariantResponseDto?>;

    // --- Command Handler ---
    public class UpdateProductVariantCommandHandler : IRequestHandler<UpdateProductVariantCommand, ProductVariantResponseDto?>
    {
        private readonly AppDbContext _context;

        public UpdateProductVariantCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProductVariantResponseDto?> Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
        {
            // 1. Find existing variant including its Inventory record
            var variant = await _context.ProductVariants
                .Include(v => v.Inventory)
                .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

            if (variant == null)
            {
                return null; // Variant not found
            }

            var dto = request.Dto;

            // 2. Update entity properties
            variant.Title = dto.Title;
            variant.Sku = dto.Sku;
            variant.ImageUrl = dto.ImageUrl;
            variant.Size = dto.Size;
            variant.Color = dto.Color;
            variant.IsActive = dto.IsActive;
            variant.UpdatedAt = DateTime.UtcNow;

            // 3. Save changes
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Return mapped Response DTO (StockQuantity removed)
            return new ProductVariantResponseDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                Title = variant.Title,
                Sku = variant.Sku,
                AvailableQuantity = variant.Inventory?.AvailableQuantity ?? 0,
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