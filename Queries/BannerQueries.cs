using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    public class BannerQueries
    {
        private readonly AppDbContext _context;

        public BannerQueries(AppDbContext context)
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
        /// Retrieves all banners, optionally filtered by position or active status.
        /// </summary>
        public async Task<IEnumerable<BannerResponseDto>> GetAllBannersAsync(string? position = null, bool? isActiveOnly = null)
        {
            var whereClauses = new List<string>();
            var dynamicParameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(position))
            {
                whereClauses.Add("Position = @Position");
                dynamicParameters.Add("Position", position);
            }

            if (isActiveOnly.HasValue && isActiveOnly.Value)
            {
                whereClauses.Add("IsActive = 1 AND (StartsAt IS NULL OR StartsAt <= @CurrentTime) AND (ExpiresAt IS NULL OR ExpiresAt >= @CurrentTime)");
                dynamicParameters.Add("CurrentTime", DateTime.UtcNow);
            }

            string whereSql = whereClauses.Count > 0
                ? " WHERE " + string.Join(" AND ", whereClauses)
                : string.Empty;

            string sql = $@"
                SELECT 
                    Id,
                    Title,
                    Subtitle,
                    ImageUrl,
                    TargetUrl,
                    Position,
                    DisplayOrder,
                    IsActive,
                    StartsAt,
                    ExpiresAt,
                    CreatedAt
                FROM banners
                {whereSql}
                ORDER BY DisplayOrder ASC, CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<BannerResponseDto>(sql, dynamicParameters);
        }

        /// <summary>
        /// Retrieves a single banner by its unique ID.
        /// </summary>
        public async Task<BannerResponseDto?> GetByIdAsync(long id)
        {
            const string sql = @"
                SELECT 
                    Id,
                    Title,
                    Subtitle,
                    ImageUrl,
                    TargetUrl,
                    Position,
                    DisplayOrder,
                    IsActive,
                    StartsAt,
                    ExpiresAt,
                    CreatedAt
                FROM banners
                WHERE Id = @Id;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<BannerResponseDto>(sql, new { Id = id });
        }
    }
}