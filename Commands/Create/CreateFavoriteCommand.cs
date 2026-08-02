using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    public record CreateFavoriteCommand(
        long UserId,
        long ProductId
    ) : IRequest<FavoriteResponseDto>;

    public class CreateFavoriteCommandHandler : IRequestHandler<CreateFavoriteCommand, FavoriteResponseDto>
    {
        private readonly AppDbContext _context;

        public CreateFavoriteCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FavoriteResponseDto> Handle(CreateFavoriteCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify Product exists
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.ProductId} was not found.");
            }

            // 2. Check if already favorited
            var exists = await _context.Favorites
                .AnyAsync(f => f.UserId == request.UserId && f.ProductId == request.ProductId, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("Product is already in your favorites.");
            }

            // 3. Create Favorite Entity
            var favorite = new Favorite
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Return DTO Response
            return new FavoriteResponseDto
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                ProductId = favorite.ProductId,
                CreatedAt = favorite.CreatedAt,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                ProductImageUrl = product.ImageUrl
            };
        }
    }
}