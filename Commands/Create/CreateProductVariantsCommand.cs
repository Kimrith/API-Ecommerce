using MediatR;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using API_Ecommerce.Data; // Adjust to your DbContext namespace
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

            // 2. Map DTO to Entity
            var variant = new ProductVariants
            {
                ProductId = dto.ProductId,
                Title = dto.Title,
                Sku = dto.Sku,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                StockQuantity = dto.StockQuantity,
                ImageUrl = dto.ImageUrl,
                Size = dto.Size,
                Color = dto.Color,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            // 3. Save to database
            await _context.ProductVariants.AddAsync(variant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Map Entity to Response DTO
            return new ProductVariantResponseDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                Title = variant.Title,
                Sku = variant.Sku,
                Price = variant.Price,
                DiscountPrice = variant.DiscountPrice,
                StockQuantity = variant.StockQuantity,
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