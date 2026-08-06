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
            // 1. Fetch product along with its inventory, variants, and variant inventories
            var product = await _context.Products
                .Include(p => p.Inventory)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.Id} was not found.");
            }

            // 2. Authorization check
            bool isAdmin = request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            if (product.SellerId != request.UserId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this product.");
            }

            // 3. Clean up physical image file
            DeleteImageFile(product.ImageUrl);

            // 4. Remove all referencing child records to bypass foreign key locks
            var orderItems = await _context.OrderItems.Where(oi => oi.ProductId == request.Id).ToListAsync(cancellationToken);
            _context.OrderItems.RemoveRange(orderItems);

            var favorites = await _context.Favorites.Where(f => f.ProductId == request.Id).ToListAsync(cancellationToken);
            _context.Favorites.RemoveRange(favorites);

            var reviews = await _context.Reviews.Where(r => r.ProductId == request.Id).ToListAsync(cancellationToken);
            _context.Reviews.RemoveRange(reviews);

            var cartItems = await _context.CartItems.Where(ci => ci.ProductId == request.Id).ToListAsync(cancellationToken);
            _context.CartItems.RemoveRange(cartItems);

            // 5. Remove product-level inventory if any
            if (product.Inventory != null)
            {
                _context.Entry(product.Inventory).State = EntityState.Deleted;
            }

            // 6. Remove variant-level inventories first, then variants
            foreach (var variant in product.Variants)
            {
                if (variant.Inventory != null)
                {
                    _context.Entry(variant.Inventory).State = EntityState.Deleted;
                }
            }
            _context.ProductVariants.RemoveRange(product.Variants);

            // 7. Finally, remove the product itself and save changes
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
                    // Suppress error if file cleanup fails so database workflow succeeds
                }
            }
        }
    }
}