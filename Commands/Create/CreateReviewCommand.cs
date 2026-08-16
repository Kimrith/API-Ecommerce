using MediatR;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using API_Ecommerce.Data;

namespace API_Ecommerce.Commands.Create
{
    // 1. Update the Command to carry the required data and return the Response DTO
    public class CreateReviewCommand : IRequest<ReviewResponseDto>
    {
        public long ProductId { get; set; }
        public long UserId { get; set; }
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
    }

    // 2. Create the Command Handler to handle the business logic and database persistence
    public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewResponseDto>
    {
        private readonly AppDbContext _context; // Replace with your actual DbContext name

        public CreateReviewCommandHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReviewResponseDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
        {
            // Optional: Check if the product or user exists, or if they already reviewed this item.

            var review = new Review
            {
                ProductId = request.ProductId,
                UserId = request.UserId,
                Rating = request.Rating,
                Title = request.Title,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow,
                IsApproved = true // Default value from model
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync(cancellationToken);

            // Map and return the response DTO
            return new ReviewResponseDto
            {
                Id = review.Id,
                ProductId = review.ProductId,
                UserId = review.UserId,
                Rating = review.Rating,
                Title = review.Title,
                Comment = review.Comment,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                CreatedAt = review.CreatedAt
            };
        }
    }
}