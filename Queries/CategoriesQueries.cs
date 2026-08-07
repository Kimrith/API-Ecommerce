using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    public class CategoriesQueries
    {
        private readonly AppDbContext _context;

        public CategoriesQueries(AppDbContext context)
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
        /// Retrieves all categories with optional status filter, creator details, and product counts.
        /// </summary>
        public async Task<IEnumerable<CategoryResponseDto>> GetAllCategoriesAsync(CategoriesStatus? status = null)
        {
            var sql = @"
                SELECT 
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.Status,
                    c.Description,
                    c.ImageUrl,
                    c.UserId,
                    COALESCE(u.FullName, 'Unknown') AS CreatedBy,
                    c.CreatedAt,
                    c.UpdatedAt,
                    COUNT(p.Id) AS ProductCount
                FROM Categories c
                LEFT JOIN Auths u ON c.UserId = u.Id
                LEFT JOIN Products p ON c.Id = p.CategoryId";

            object? parameters = null;

            if (status.HasValue)
            {
                // Handles both DB storage types: integer ID (0,1,2) or string name ("Pending")
                sql += " WHERE (c.Status = @StatusValue OR LOWER(CAST(c.Status AS NVARCHAR(50))) = LOWER(@StatusName))";
                parameters = new
                {
                    StatusValue = (int)status.Value,
                    StatusName = status.Value.ToString()
                };
            }

            sql += @" 
                GROUP BY 
                    c.Id, c.Name, c.Slug, c.Status, c.Description, 
                    c.ImageUrl, c.UserId, u.FullName, c.CreatedAt, c.UpdatedAt
                ORDER BY c.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<CategoryResponseDto>(sql, parameters);
        }

        /// <summary>
        /// Retrieves a single category by ID with creator details and product count.
        /// </summary>
        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT 
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.Status,
                    c.Description,
                    c.ImageUrl,
                    c.UserId,
                    COALESCE(u.FullName, 'Unknown') AS CreatedBy,
                    c.CreatedAt,
                    c.UpdatedAt,
                    COUNT(p.Id) AS ProductCount
                FROM Categories c
                LEFT JOIN Auths u ON c.UserId = u.Id
                LEFT JOIN Products p ON c.Id = p.CategoryId
                WHERE c.Id = @Id
                GROUP BY 
                    c.Id, c.Name, c.Slug, c.Status, c.Description, 
                    c.ImageUrl, c.UserId, u.FullName, c.CreatedAt, c.UpdatedAt;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<CategoryResponseDto>(sql, new { Id = id });
        }

        /// <summary>
        /// Retrieves categories created by a specific user/seller ID.
        /// </summary>
        public async Task<IEnumerable<CategoryResponseDto>> GetByUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT 
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.Status,
                    c.Description,
                    c.ImageUrl,
                    c.UserId,
                    COALESCE(u.FullName, 'Unknown') AS CreatedBy,
                    c.CreatedAt,
                    c.UpdatedAt,
                    COUNT(p.Id) AS ProductCount
                FROM Categories c
                LEFT JOIN Auths u ON c.UserId = u.Id
                LEFT JOIN Products p ON c.Id = p.CategoryId
                WHERE c.UserId = @UserId
                GROUP BY 
                    c.Id, c.Name, c.Slug, c.Status, c.Description, 
                    c.ImageUrl, c.UserId, u.FullName, c.CreatedAt, c.UpdatedAt
                ORDER BY c.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<CategoryResponseDto>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Retrieves statistics on product statuses across categories.
        /// </summary>
        public async Task<CategoriesStatistics> GetCategoriesStatisticsAsync()
        {
            const string sql = @"
                SELECT 
                    COUNT(c.Id) AS totalCategories,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 0 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'draft' THEN 1 ELSE 0 END) AS Draft,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 1 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'pending' THEN 1 ELSE 0 END) AS Pending,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 2 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'approved' THEN 1 ELSE 0 END) AS Approved,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 3 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'rejected' THEN 1 ELSE 0 END) AS Rejected,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 4 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'archived' THEN 1 ELSE 0 END) AS Archived,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 5 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'suspended' THEN 1 ELSE 0 END) AS Suspended
                FROM Categories c;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<CategoriesStatistics>(sql) ?? new CategoriesStatistics();
        }
    }
}