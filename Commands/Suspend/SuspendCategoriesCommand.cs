using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    // --- 1. Command Record ---
    public record SuspendCategoriesCommand(
        int CategoryId,
        CategoriesStatus TargetStatus, // e.g., CategoriesStatus.Archived or CategoriesStatus.Rejected
        int UserId,
        string UserRole
    ) : IRequest<CategoryResponseDto>;

    // --- 2. Command Handler ---
    public class SuspendCategoriesCommandHandler : IRequestHandler<SuspendCategoriesCommand, CategoryResponseDto>
    {
        private readonly AppDbContext _context;

        public SuspendCategoriesCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CategoryResponseDto> Handle(SuspendCategoriesCommand request, CancellationToken cancellationToken)
        {
            // 1. Find the target category
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {request.CategoryId} was not found.");
            }

            // 2. Authorization / Permission checks
            bool isAdmin = request.UserRole.Equals(Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                // Sellers can only suspend/archive categories they personally created
                if (category.UserId != request.UserId)
                {
                    throw new UnauthorizedAccessException("You do not have permission to modify this category.");
                }

                // Non-admins should only be able to toggle between Active/Archived or Draft
                if (request.TargetStatus == CategoriesStatus.Approved || request.TargetStatus == CategoriesStatus.Rejected)
                {
                    throw new InvalidOperationException("Only administrators can approve or reject categories.");
                }
            }

            // 3. Update Status and Timestamp
            category.Status = request.TargetStatus;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Retrieve Creator details for response DTO
            var creatorName = await _context.Auths
                .Where(u => u.Id == category.UserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

            // 5. Return updated response
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
    }
}