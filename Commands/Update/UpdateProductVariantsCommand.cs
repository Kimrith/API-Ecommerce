using MediatR;
using API_Ecommerce.DTOs;
using API_Ecommerce.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace API_Ecommerce.Commands.Update
{
    // --- Request Command ---
    public record UpdateProductVariantCommand(long Id, UpdateProductVariantDto Dto) : IRequest<ProductVariantResponseDto?>;

    // --- Command Handler ---
    public class UpdateProductVariantCommandHandler : IRequestHandler<UpdateProductVariantCommand, ProductVariantResponseDto?>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UpdateProductVariantCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

            // 2. Handle Image File Upload (If a new file is provided)
            if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "variants");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete the old image file from disk if it exists
                if (!string.IsNullOrEmpty(variant.ImageUrl))
                {
                    string oldFilePath = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), variant.ImageUrl.TrimStart('/', '\\'));
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                // Save the new image file
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.ImageUrl.FileName)}";
                string newFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                {
                    await dto.ImageUrl.CopyToAsync(fileStream, cancellationToken);
                }

                // Update the string path in the model
                variant.ImageUrl = $"/images/variants/{uniqueFileName}";
            }
            // Note: If dto.ImageUrl is null, we keep the existing variant.ImageUrl intact.

            // 3. Update remaining entity properties
            variant.Title = dto.Title;
            variant.Sku = dto.Sku;
            variant.Size = dto.Size;
            variant.Color = dto.Color;
            variant.Price = dto.Price;
            variant.DiscountPrice = dto.DiscountPrice;
            variant.InitialStock = dto.InitialStock;
            variant.IsActive = dto.IsActive;
            variant.UpdatedAt = DateTime.UtcNow;

            // 4. Save changes
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Return mapped Response DTO
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
                Price = variant.Price,
                DiscountPrice = variant.DiscountPrice,
                InitialStock = variant.InitialStock,
                IsActive = variant.IsActive,
                CreatedAt = variant.CreatedAt,
                UpdatedAt = variant.UpdatedAt
            };
        }
    }
}