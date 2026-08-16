using MediatR;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using API_Ecommerce.Data;

namespace API_Ecommerce.Commands.Update
{
    // 1. The Command carrying the ID, update payload, and authorization details
    public class UpdateReviewCommand : IRequest<ReviewResponseDto?>
    {
        public long Id { get; set; }
        public long UserId { get; set; } // Used to ensure users can only update their own reviews
        public bool IsAdmin { get; set; } // Optional flag if admins can bypass ownership rules

        // Fields from UpdateReviewDto
        public int? Rating { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
        public bool? IsApproved { get; set; } // Optional: for admin moderation
    }

    // 2. The Handler that updates the review in the database
    public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewResponseDto?>
    {
        private readonly AppDbContext _context; // Replace with your actual DbContext name

        public UpdateReviewCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReviewResponseDto?> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
        {
            var review = await _context.Reviews.FindAsync(new object[] { request.Id }, cancellationToken);

            if (review == null)
            {
                return null; // Review not found
            }

            // Optional Security Check: Ensure the user owns the review or is an admin
            if (!request.IsAdmin && review.UserId != request.UserId)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this review.");
            }

            // Apply updates only if the values are provided (partial update support)
            if (request.Rating.HasValue)
            {
                review.Rating = request.Rating.Value;
            }

            if (request.Title != null)
            {
                review.Title = request.Title;
            }

            if (request.Comment != null)
            {
                review.Comment = request.Comment;
            }

            // Typically only admins should update IsApproved status
            if (request.IsApproved.HasValue && request.IsAdmin)
            {
                review.IsApproved = request.IsApproved.Value;
            }

            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Map and return the updated response DTO
            return new ReviewResponseDto
            {
                Id = review.Id,
                ProductId = review.ProductId,
                UserId = review.UserId,
                Rating = review.Rating,
                Title = review.Title,
                Comment = review.Comment,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };
        }
    }
}