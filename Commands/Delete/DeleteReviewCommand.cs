using MediatR;
using API_Ecommerce.Models;
using API_Ecommerce.Data;

namespace API_Ecommerce.Commands.Delete
{
    // 1. The Command carrying the ID of the review to delete (and optionally the requesting user for authorization)
    public class DeleteReviewCommand : IRequest<bool>
    {
        public long Id { get; set; }
        public long UserId { get; set; } // Used to ensure users can only delete their own reviews (unless they are admin)
        public bool IsAdmin { get; set; } // Optional flag if admins can delete any review
    }

    // 2. The Handler that executes the delete operation in the database
    public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, bool>
    {
        private readonly AppDbContext _context; // Replace with your actual DbContext name

        public DeleteReviewCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
        {
            var review = await _context.Reviews.FindAsync(new object[] { request.Id }, cancellationToken);

            if (review == null)
            {
                return false; // Review not found
            }

            // Optional Security Check: Ensure the user owns the review or is an admin
            if (!request.IsAdmin && review.UserId != request.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this review.");
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync(cancellationToken);

            return true; // Successfully deleted
        }
    }
}