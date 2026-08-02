using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    public class FavoriteQueries
    {
        private readonly AppDbContext _context;

        public FavoriteQueries(AppDbContext context)
        {
            _context = context;
        }

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
        /// Retrieves all favorite products for a specific user with product details.
        /// </summary>
        public async Task<IEnumerable<FavoriteResponseDto>> GetFavoritesByUserIdAsync(long userId)
        {
            const string sql = @"
                SELECT 
                    f.Id,
                    f.UserId,
                    f.ProductId,
                    f.CreatedAt,
                    COALESCE(p.Name, '') AS ProductName,
                    COALESCE(p.Slug, '') AS ProductSlug,
                    COALESCE(p.Price, 0) AS Price,
                    p.DiscountPrice,
                    COALESCE(p.ImageUrl, '') AS ProductImageUrl
                FROM favorites f
                INNER JOIN Products p ON f.ProductId = p.Id
                WHERE f.UserId = @UserId
                ORDER BY f.CreatedAt DESC;";

            using var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<FavoriteResponseDto>(sql, new { UserId = userId });
        }
    }
}