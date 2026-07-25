using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    // --- 1. Command Record ---
    public record UpdateCategoriesCommand(
        int CategoryId,
        UpdateCategoryDto Dto,
        int UserId,
        string UserRole
    ) : IRequest<CategoryResponseDto>;

    // --- 2. Command Handler ---
    public class UpdateCategoriesCommandHandler : IRequestHandler<UpdateCategoriesCommand, CategoryResponseDto>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UpdateCategoriesCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<CategoryResponseDto> Handle(UpdateCategoriesCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // 1. Fetch category from database
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {request.CategoryId} was not found.");
            }

            // 2. Authorization Check:
            // Non-admin sellers can only update categories they personally created
            bool isAdmin = request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && category.UserId != request.UserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this category.");
            }

            // 3. Name & Slug Update (check for duplicates if name changed)
            if (!category.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var nameExists = await _context.Categories
                    .AnyAsync(c => c.Id != request.CategoryId && c.Name.ToLower() == dto.Name.ToLower(), cancellationToken);

                if (nameExists)
                {
                    throw new InvalidOperationException($"Another category with the name '{dto.Name}' already exists.");
                }

                category.Name = dto.Name;
                category.Slug = await GenerateUniqueSlugAsync(dto.Name, request.CategoryId, cancellationToken);
            }

            // 4. Update Status (Sellers cannot set to Approved/Rejected without Admin rights)
            if (!isAdmin && (dto.Status == CategoriesStatus.Approved || dto.Status == CategoriesStatus.Rejected))
            {
                throw new InvalidOperationException("Only administrators can approve or reject categories.");
            }
            category.Status = dto.Status;

            // 5. Update Description
            category.Description = dto.Description;

            // 6. Handle New Image Upload (and delete old image file)
            if (dto.Image != null && dto.Image.Length > 0)
            {
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    DeleteCategoryImageFile(category.ImageUrl);
                }

                category.ImageUrl = await SaveImageAsync(dto.Image);
            }

            // 7. Update Metadata and Save
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // 8. Fetch Creator Full Name
            var creatorName = await _context.Auths
                .Where(u => u.Id == category.UserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

            // 9. Return Response DTO
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Status = category.Status,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                ProductCount = category.Products?.Count ?? 0,
                UserId = category.UserId,
                CreatedBy = creatorName,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }

        // --- Helper: Slug Generator ---
        private async Task<string> GenerateUniqueSlugAsync(string name, int categoryId, CancellationToken cancellationToken)
        {
            string baseSlug = name.ToLower().Trim().Replace(" ", "-").Replace("&", "and");
            string slug = baseSlug;
            int count = 1;

            while (await _context.Categories.AnyAsync(c => c.Slug == slug && c.Id != categoryId, cancellationToken))
            {
                slug = $"{baseSlug}-{count++}";
            }

            return slug;
        }

        // --- Helper: Save Image File ---
        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "categories");

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

            return $"/uploads/categories/{uniqueFileName}";
        }

        // --- Helper: Delete Old Image File ---
        private void DeleteCategoryImageFile(string imageUrl)
        {
            try
            {
                var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var relativePath = imageUrl.TrimStart('/', '\\');
                var filePath = Path.Combine(webRoot, relativePath);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Silently handle if file deletion fails to prevent blocking DB save
            }
        }
    }
}