using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
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

            using var connection = await GetOpenConnectionAsync();
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

            using var connection = await GetOpenConnectionAsync();
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

            using var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<CategoryResponseDto>(sql, new { UserId = userId });
        }
    }
}