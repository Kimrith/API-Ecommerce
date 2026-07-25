using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    // --- 1. Command Record ---
    public record CreateCategoriesCommand(
        CreateCategoryDto Dto,
        int UserId,
        string UserRole
    ) : IRequest<CategoryResponseDto>;

    // --- 2. Command Handler ---
    public class CreateCategoriesCommandHandler : IRequestHandler<CreateCategoriesCommand, CategoryResponseDto>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateCategoriesCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<CategoryResponseDto> Handle(CreateCategoriesCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // 1. Check if Category Name already exists
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower(), cancellationToken);

            if (existingCategory != null)
            {
                throw new InvalidOperationException($"Category with name '{dto.Name}' already exists.");
            }

            // 2. Generate unique slug
            string baseSlug = GenerateSlug(dto.Name);
            string slug = baseSlug;
            int count = 1;

            while (await _context.Categories.AnyAsync(c => c.Slug == slug, cancellationToken))
            {
                slug = $"{baseSlug}-{count++}";
            }

            // 3. Handle Image Upload if provided
            string? imageUrl = null;
            if (dto.Image != null && dto.Image.Length > 0)
            {
                imageUrl = await SaveImageAsync(dto.Image);
            }

            // 4. Determine initial status based on role
            // Admins can auto-approve; Sellers are defaulted to Pending (or Draft if explicitly passed)
            CategoriesStatus initialStatus = dto.Status;
            if (request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                initialStatus = CategoriesStatus.Approved;
            }

            // 5. Map DTO to Entity
            var category = new Categories
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                ImageUrl = imageUrl,
                Status = initialStatus,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };

            // 6. Save to Database
            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            // 7. Retrieve Creator Name for Response
            var creator = await _context.Auths
                .Where(u => u.Id == request.UserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

            // 8. Return Response DTO
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Status = category.Status,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                ProductCount = 0,
                UserId = category.UserId,
                CreatedBy = creator,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
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
    }
}