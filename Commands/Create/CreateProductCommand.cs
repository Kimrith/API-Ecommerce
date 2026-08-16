using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    // --- 1. Command Record ---
    public record CreateProductCommand(
        CreateProductDto Dto,
        int SellerId,
        string UserRole
    ) : IRequest<ProductResponseDto>;

    // --- 2. Command Handler ---
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductResponseDto>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateProductCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<ProductResponseDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // 1. Validate Category exists
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId, cancellationToken);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {dto.CategoryId} was not found.");
            }

            // 2. Validate Seller exists
            var seller = await _context.Auths
                .FirstOrDefaultAsync(u => u.Id == request.SellerId, cancellationToken);

            if (seller == null)
            {
                throw new KeyNotFoundException($"Seller with ID {request.SellerId} was not found.");
            }

            // 3. Generate unique slug for Product
            string baseSlug = GenerateSlug(dto.Name);
            string slug = baseSlug;
            int count = 1;

            while (await _context.Products.AnyAsync(p => p.Slug == slug, cancellationToken))
            {
                slug = $"{baseSlug}-{count++}";
            }

            // 4. Handle Image Upload if provided
            string imageUrl = "/uploads/products/default.png";
            if (dto.Image != null && dto.Image.Length > 0)
            {
                imageUrl = await SaveImageAsync(dto.Image);
            }

            // 5. Determine initial status based on role
            // Admins can auto-approve; Sellers default to Pending
            ProductStatus initialStatus = ProductStatus.Pending;
            if (request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                initialStatus = ProductStatus.Approved;
            }

            // 6. Map DTO to Entity (StockQuantity removed from Product)
            var product = new Product
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                DiscountStartDate = dto.DiscountStartDate,
                DiscountEndDate = dto.DiscountEndDate,
                Size = dto.Size,
                Color = dto.Color,
                CategoryId = dto.CategoryId,
                SellerId = request.SellerId,
                ImageUrl = imageUrl,
                Status = initialStatus,
                PublishAt = dto.PublishAt ?? DateTime.UtcNow, // Default to now if not scheduled
                CreatedAt = DateTime.UtcNow
            };

            // 7. Save Product to Database to get its Id
            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

            // 8. Create the Inventory record using InitialStock
            var inventory = new Inventory
            {
                ProductId = product.Id,
                VariantId = null, // No variants for base product creation
                Quantity = dto.InitialStock,
                ReservedQuantity = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync(cancellationToken);

            // 9. Return Response DTO pulling from Inventory
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                DiscountStartDate = product.DiscountStartDate,
                DiscountEndDate = product.DiscountEndDate,
                StockQuantity = inventory.Quantity,
                AvailableQuantity = inventory.AvailableQuantity,
                Size = product.Size,
                Color = product.Color,
                ImageUrl = product.ImageUrl,
                Status = product.Status,
                PublishAt = product.PublishAt,
                CategoryId = product.CategoryId,
                CategoryName = category.Name,
                SellerId = product.SellerId,
                SellerName = seller.FullName,
                SellerRole = seller.Role.ToString(),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        // --- Helper: Slug Generator ---
        private static string GenerateSlug(string name)
        {
            return name.ToLower()
                .Trim()
                .Replace(" ", "-")
                .Replace("&", "and");
        }

        // --- Helper: Image Saver ---
        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "products");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return $"/uploads/products/{uniqueFileName}";
        }
    }
}