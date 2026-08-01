using API_Ecommerce.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Delete
{
    // 1. COMMAND RECORD
    public record DeleteCouponCommand(long Id) : IRequest<bool>;

    // 2. COMMAND HANDLER
    public class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, bool>
    {
        private readonly AppDbContext _context;

        public DeleteCouponCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
        {
            // Find the coupon by ID
            var coupon = await _context.Coupons
                .Include(c => c.CouponUsages) // Fixed: changed from .Usages to .CouponUsages
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken); // Fixed: using request.Id correctly

            if (coupon == null)
            {
                throw new KeyNotFoundException($"Coupon with ID {request.Id} was not found.");
            }

            // Optional: Check if the coupon has already been used before deleting, 
            // or if you want to prevent deleting coupons with history.
            if (coupon.CouponUsages != null && coupon.CouponUsages.Any())
            {
                // Option A: Throw an error if it's already used
                throw new InvalidOperationException("Cannot delete a coupon that has already been used by customers. Consider deactivating it instead.");

                // Option B: If your database has cascade delete set up, you can let it delete automatically.
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}