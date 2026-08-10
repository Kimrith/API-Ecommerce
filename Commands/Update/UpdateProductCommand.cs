using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    // --- 1. Command Record ---
    public record UpdateProductCommand(
        int Id,
        UpdateProductDto Dto,
        int UserId,
        string UserRole
    ) : IRequest<ProductResponseDto>;

    // --- 2. Command Handler ---
    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductResponseDto>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UpdateProductCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<ProductResponseDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // 1. Fetch product with related entities (including Inventory)
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.Id} was not found.");
            }

            // 2. Authorization check: Sellers can only update their own products unless they are an Admin
            bool isAdmin = request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            if (product.SellerId != request.UserId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this product.");
            }

            // 3. Validate Category exists if changed
            if (product.CategoryId != dto.CategoryId)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);

                if (!categoryExists)
                {
                    throw new KeyNotFoundException($"Category with ID {dto.CategoryId} was not found.");
                }
                product.CategoryId = dto.CategoryId;
            }

            // 4. Update Slug if Name changed
            if (!product.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                string baseSlug = GenerateSlug(dto.Name);
                string slug = baseSlug;
                int count = 1;

                while (await _context.Products.AnyAsync(p => p.Slug == slug && p.Id != product.Id, cancellationToken))
                {
                    slug = $"{baseSlug}-{count++}";
                }

                product.Name = dto.Name;
                product.Slug = slug;
            }

            // 5. Replace Image if new file provided
            if (dto.Image != null && dto.Image.Length > 0)
            {
                DeleteImageFile(product.ImageUrl);
                product.ImageUrl = await SaveImageAsync(dto.Image);
            }

            // 6. Update remaining fields (StockQuantity is managed via Inventory model)
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.DiscountPrice = dto.DiscountPrice;
            product.DiscountStartDate = dto.DiscountStartDate;
            product.DiscountEndDate = dto.DiscountEndDate;
            product.Status = dto.Status;

            if (product.Inventory != null)
            {
                product.Inventory.Quantity = dto.InitialStock;
                product.Inventory.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Fallback: If for some reason inventory is null, create it
                product.Inventory = new Inventory
                {
                    ProductId = product.Id,
                    Quantity = dto.InitialStock,
                    ReservedQuantity = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Inventories.Add(product.Inventory);
            }

            if (dto.PublishAt.HasValue)
            {
                product.PublishAt = dto.PublishAt.Value;
            }

            product.UpdatedAt = DateTime.UtcNow;

            // 7. Save changes
            await _context.SaveChangesAsync(cancellationToken);

            // Re-fetch Category and Seller names if needed for fresh display
            var categoryName = await _context.Categories
                .Where(c => c.Id == product.CategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            var sellerName = product.Seller?.FullName ?? "Unknown";

            // 8. Return Response DTO utilizing Inventory
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
                StockQuantity = product.Inventory?.Quantity ?? 0,
                AvailableQuantity = product.Inventory?.AvailableQuantity ?? 0,
                ImageUrl = product.ImageUrl,
                Status = product.Status,
                PublishAt = product.PublishAt,
                CategoryId = product.CategoryId,
                CategoryName = categoryName,
                SellerId = product.SellerId,
                SellerName = sellerName,
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

        // --- Helper: Save Image ---
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

        // --- Helper: Delete Old Image ---
        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.Contains("default.png"))
            {
                return;
            }

            var relativePath = imageUrl.TrimStart('/', '\\');
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRoot, relativePath);

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Suppress error if file cleanup fails
                }
            }
        }
    }
}