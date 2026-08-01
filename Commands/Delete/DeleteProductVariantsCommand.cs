using MediatR;
using API_Ecommerce.Data; // Adjust to your DbContext namespace
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Delete
{
    // --- Request Command ---
    public record DeleteProductVariantCommand(long Id) : IRequest<bool>;

    // --- Command Handler ---
    public class DeleteProductVariantCommandHandler : IRequestHandler<DeleteProductVariantCommand, bool>
    {
        private readonly AppDbContext _context;

        public DeleteProductVariantCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
        {
            // 1. Fetch the variant
            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

            if (variant == null)
            {
                return false; // Variant not found
            }

            // 2. Remove and save
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}