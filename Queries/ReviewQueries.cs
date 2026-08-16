using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    // --- 1. MediatR Query Classes ---

    public class GetReviewsByProductIdQuery : IRequest<IEnumerable<ReviewResponseDto>>
    {
        public long ProductId { get; set; }
    }

    public class GetReviewByIdQuery : IRequest<ReviewResponseDto?>
    {
        public long Id { get; set; }
    }

    public class GetReviewsByUserIdQuery : IRequest<IEnumerable<ReviewResponseDto>>
    {
        public long UserId { get; set; }
    }

    // NEW: Query for getting all reviews (e.g., for Admin panel or general feeds)
    public class GetAllReviewsQuery : IRequest<IEnumerable<ReviewResponseDto>>
    {
        public bool? IsApproved { get; set; } // Optional filter: null = all, true = approved only, false = unapproved only
    }


    // --- 2. Query Service Layer (Dapper Execution) ---

    public class ReviewQueries
    {
        private readonly AppDbContext _context;

        public ReviewQueries(AppDbContext context)
        {
            _context = context;
        }

        // Helper to get and automatically open connection if closed
        private async Task<IDbConnection> GetOpenConnectionAsync()
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            return connection;
        }

        /// <summary>
        /// Retrieves all reviews across the system with an optional approval filter.
        /// </summary>
        public async Task<IEnumerable<ReviewResponseDto>> GetAllReviewsAsync(bool? isApproved = null)
        {
            var whereClause = string.Empty;
            var parameters = new DynamicParameters();

            if (isApproved.HasValue)
            {
                whereClause = "WHERE r.IsApproved = @IsApproved";
                parameters.Add("IsApproved", isApproved.Value);
            }

            var sql = $@"
                SELECT 
                    r.Id,
                    r.ProductId,
                    p.Name AS ProductName,
                    r.UserId,
                    u.FullName AS UserName,
                    r.Rating,
                    r.Title,
                    r.Comment,
                    r.IsVerifiedPurchase,
                    r.CreatedAt,
                    r.UpdatedAt
                FROM reviews r
                INNER JOIN Products p ON r.ProductId = p.Id
                INNER JOIN Auths u ON r.UserId = u.Id
                {whereClause}
                ORDER BY r.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<ReviewResponseDto>(sql, parameters);
        }

        /// <summary>
        /// Retrieves all reviews for a specific product, including user names.
        /// </summary>
        public async Task<IEnumerable<ReviewResponseDto>> GetReviewsByProductIdAsync(long productId)
        {
            const string sql = @"
                SELECT 
                    r.Id,
                    r.ProductId,
                    p.Name AS ProductName,
                    r.UserId,
                    u.FullName AS UserName,
                    r.Rating,
                    r.Title,
                    r.Comment,
                    r.IsVerifiedPurchase,
                    r.CreatedAt,
                    r.UpdatedAt
                FROM reviews r
                INNER JOIN Products p ON r.ProductId = p.Id
                INNER JOIN Auths u ON r.UserId = u.Id
                WHERE r.ProductId = @ProductId AND r.IsApproved = 1
                ORDER BY r.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<ReviewResponseDto>(sql, new { ProductId = productId });
        }

        /// <summary>
        /// Retrieves a single review by ID.
        /// </summary>
        public async Task<ReviewResponseDto?> GetReviewByIdAsync(long id)
        {
            const string sql = @"
                SELECT 
                    r.Id,
                    r.ProductId,
                    p.Name AS ProductName,
                    r.UserId,
                    u.FullName AS UserName,
                    r.Rating,
                    r.Title,
                    r.Comment,
                    r.IsVerifiedPurchase,
                    r.CreatedAt,
                    r.UpdatedAt
                FROM reviews r
                INNER JOIN Products p ON r.ProductId = p.Id
                INNER JOIN Auths u ON r.UserId = u.Id
                WHERE r.Id = @Id;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<ReviewResponseDto>(sql, new { Id = id });
        }

        /// <summary>
        /// Retrieves all reviews written by a specific user.
        /// </summary>
        public async Task<IEnumerable<ReviewResponseDto>> GetReviewsByUserIdAsync(long userId)
        {
            const string sql = @"
                SELECT 
                    r.Id,
                    r.ProductId,
                    p.Name AS ProductName,
                    r.UserId,
                    u.FullName AS UserName,
                    r.Rating,
                    r.Title,
                    r.Comment,
                    r.IsVerifiedPurchase,
                    r.CreatedAt,
                    r.UpdatedAt
                FROM reviews r
                INNER JOIN Products p ON r.ProductId = p.Id
                INNER JOIN Auths u ON r.UserId = u.Id
                WHERE r.UserId = @UserId
                ORDER BY r.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<ReviewResponseDto>(sql, new { UserId = userId });
        }
    }


    // --- 3. MediatR Query Handlers ---

    public class ReviewQueryHandlers :
        IRequestHandler<GetAllReviewsQuery, IEnumerable<ReviewResponseDto>>,
        IRequestHandler<GetReviewsByProductIdQuery, IEnumerable<ReviewResponseDto>>,
        IRequestHandler<GetReviewByIdQuery, ReviewResponseDto?>,
        IRequestHandler<GetReviewsByUserIdQuery, IEnumerable<ReviewResponseDto>>
    {
        private readonly ReviewQueries _reviewQueries;

        public ReviewQueryHandlers(ReviewQueries reviewQueries)
        {
            _reviewQueries = reviewQueries;
        }

        public async Task<IEnumerable<ReviewResponseDto>> Handle(GetAllReviewsQuery request, CancellationToken cancellationToken)
        {
            return await _reviewQueries.GetAllReviewsAsync(request.IsApproved);
        }

        public async Task<IEnumerable<ReviewResponseDto>> Handle(GetReviewsByProductIdQuery request, CancellationToken cancellationToken)
        {
            return await _reviewQueries.GetReviewsByProductIdAsync(request.ProductId);
        }

        public async Task<ReviewResponseDto?> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
        {
            return await _reviewQueries.GetReviewByIdAsync(request.Id);
        }

        public async Task<IEnumerable<ReviewResponseDto>> Handle(GetReviewsByUserIdQuery request, CancellationToken cancellationToken)
        {
            return await _reviewQueries.GetReviewsByUserIdAsync(request.UserId);
        }
    }
}