using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    // --- 1. Command Record ---
    public record SuspendProductCommand(
        int Id,
        ProductStatus Status, // Added dynamic Status field
        int UserId,
        string UserRole
    ) : IRequest<ProductResponseDto>;

    // --- 2. Command Handler ---
    public class SuspendProductCommandHandler : IRequestHandler<SuspendProductCommand, ProductResponseDto>
    {
        private readonly AppDbContext _context;

        public SuspendProductCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProductResponseDto> Handle(SuspendProductCommand request, CancellationToken cancellationToken)
        {
            // 1. Fetch product with category, seller, and inventory details
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.Id} was not found.");
            }

            // 2. Authorization check: Admins can update status of any product; Sellers can only update status of their own
            bool isAdmin = request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            if (product.SellerId != request.UserId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You do not have permission to update status of this product.");
            }

            // 3. Optional: Prevent duplicate state update
            if (product.Status == request.Status)
            {
                throw new InvalidOperationException($"Product status is already {request.Status}.");
            }

            // 4. Update status and timestamp
            product.Status = request.Status;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 5. Return updated ProductResponseDto pulling from Inventory
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
                CategoryName = product.Category?.Name ?? string.Empty,
                SellerId = product.SellerId,
                SellerName = product.Seller?.FullName ?? "Unknown",
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
    }
}