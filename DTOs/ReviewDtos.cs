namespace API_Ecommerce.DTOs
{
    public class ReviewResponseDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string? ProductName { get; set; } // Optional: convenient for UI displays

        public long UserId { get; set; }
        public string? UserName { get; set; } // Optional: user identifier/name

        public int Rating { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }

        public bool IsVerifiedPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateReviewDto
    {
        public long ProductId { get; set; }
        public long UserId { get; set; }
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
    }

    public class UpdateReviewDto
    {
        public int? Rating { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
        public bool? IsApproved { get; set; } // Optional: for admin moderation
    }
}