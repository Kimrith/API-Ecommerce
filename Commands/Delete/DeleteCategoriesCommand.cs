using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    // --- 1. Command Record ---
    public record DeleteCategoriesCommand(
        int CategoryId,
        int UserId,
        string UserRole
    ) : IRequest<bool>;

    // --- 2. Command Handler ---
    public class DeleteCategoriesCommandHandler : IRequestHandler<DeleteCategoriesCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DeleteCategoriesCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<bool> Handle(DeleteCategoriesCommand request, CancellationToken cancellationToken)
        {
            // 1. Fetch category from database
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {request.CategoryId} was not found.");
            }

            // 2. Ownership / Authorization Check:
            // Non-admin sellers can only delete categories they created
            bool isAdmin = request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            if (!isAdmin && category.UserId != request.UserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this category.");
            }

            // 3. Dependency Check: Prevent deletion if products are actively linked to this category
            if (category.Products.Any())
            {
                throw new InvalidOperationException($"Cannot delete category '{category.Name}' because it currently contains {category.Products.Count} product(s). Please reassign or remove the products first.");
            }

            // 4. Clean up uploaded image file if present
            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                DeleteCategoryImageFile(category.ImageUrl);
            }

            // 5. Remove entity and save changes
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        // --- Helper: File Cleanup ---
        private void DeleteCategoryImageFile(string imageUrl)
        {
            try
            {
                // Converts relative Web URL ("/uploads/categories/...") to absolute local disk path
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
                // Logging can be added here if needed; avoid throwing to ensure DB operation completes cleanly
            }
        }
    }
}