using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    // --- 1. Command Record ---
    public record DeleteProductCommand(
        int Id,
        int UserId,
        string UserRole
    ) : IRequest<bool>;

    // --- 2. Command Handler ---
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DeleteProductCommandHandler(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            // 1. Fetch the existing product
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.Id} was not found.");
            }

            // 2. Authorization check: Users can only delete their own products unless they are an Admin
            bool isAdmin = request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            if (product.SellerId != request.UserId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this product.");
            }

            // 3. Optional: Clean up associated image file from local storage
            DeleteImageFile(product.ImageUrl);

            // 4. Remove entity from DB
            _context.Products.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        // --- Helper: Delete File from Server ---
        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.Contains("default.png"))
            {
                return;
            }

            // Convert web relative path (/uploads/products/xyz.jpg) to physical disk path
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
                    // Log failure if file cleanup fails; suppress error so DB delete succeeds
                }
            }
        }
    }
}