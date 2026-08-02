using API_Ecommerce.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands
{
    public record DeleteFavoriteCommand(
        long UserId,
        long ProductId
    ) : IRequest<bool>;

    public class DeleteFavoriteCommandHandler : IRequestHandler<DeleteFavoriteCommand, bool>
    {
        private readonly AppDbContext _context;

        public DeleteFavoriteCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteFavoriteCommand request, CancellationToken cancellationToken)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == request.UserId && f.ProductId == request.ProductId, cancellationToken);

            if (favorite == null)
            {
                return false;
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}