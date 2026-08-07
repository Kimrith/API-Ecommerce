using MediatR;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using API_Ecommerce.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment

namespace API_Ecommerce.Commands.Create
{
    // --- Request Command ---
    public record CreateProductVariantCommand(CreateProductVariantDto Dto) : IRequest<ProductVariantResponseDto?>;

    // --- Command Handler ---
    public class CreateProductVariantCommandHandler : IRequestHandler<CreateProductVariantCommand, ProductVariantResponseDto?>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment; // Added to save files locally

        public CreateProductVariantCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<ProductVariantResponseDto?> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // 1. Verify that the parent product exists
            var parentProductExists = await _context.Products
                .AnyAsync(p => p.Id == dto.ProductId, cancellationToken);

            if (!parentProductExists)
            {
                return null;
            }

            // 2. Handle Image File Upload
            string? savedImageUrl = null;
            if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
            {
                // Define folder: wwwroot/images/variants
                string uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "variants");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique file name
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.ImageUrl.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ImageUrl.CopyToAsync(fileStream, cancellationToken);
                }

                // Save relative path/URL to database model
                savedImageUrl = $"/images/variants/{uniqueFileName}";
            }

            // 3. Map DTO to Entity
            var variant = new ProductVariants
            {
                ProductId = dto.ProductId,
                Title = dto.Title,
                Sku = dto.Sku,
                ImageUrl = savedImageUrl, // FIXED: Assign the string path instead of IFormFile
                Size = dto.Size,
                Color = dto.Color,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                InitialStock = dto.InitialStock,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Save variant to database to generate ID
            await _context.ProductVariants.AddAsync(variant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Create the Inventory record (FIXED: Use dto.InitialStock instead of 0)
            var inventory = new Inventory
            {
                ProductId = null,
                VariantId = variant.Id,
                Quantity = dto.InitialStock, // FIXED: Set initial quantity
                ReservedQuantity = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync(cancellationToken);

            // 6. Map Entity & Inventory to Response DTO
            return new ProductVariantResponseDto
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                Title = variant.Title,
                Sku = variant.Sku,
                AvailableQuantity = inventory.Quantity, // or inventory.AvailableQuantity depending on your Inventory model
                ImageUrl = variant.ImageUrl, // FIXED: Returns string path to client
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