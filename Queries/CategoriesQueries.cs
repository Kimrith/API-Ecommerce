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
        /// Retrieves all categories with pagination, optional status filter, creator details, and product counts.
        /// </summary>
        public async Task<object> GetAllCategoriesAsync(PaginationParamsDtos paginationParams, CategoriesStatus? status = null)
        {
            var whereClause = string.Empty;
            var parameters = new DynamicParameters();

            if (status.HasValue)
            {
                whereClause = " WHERE (c.Status = @StatusValue OR LOWER(CAST(c.Status AS NVARCHAR(50))) = LOWER(@StatusName))";
                parameters.Add("StatusValue", (int)status.Value);
                parameters.Add("StatusName", status.Value.ToString());
            }

            // Query to count total matching records
            var countSql = $"SELECT COUNT(DISTINCT c.Id) FROM Categories c {whereClause};";

            // Query for fetching the actual paged data using OFFSET-FETCH
            var dataSql = $@"
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
                {whereClause}
                GROUP BY 
                    c.Id, c.Name, c.Slug, c.Status, c.Description, 
                    c.ImageUrl, c.UserId, u.FullName, c.CreatedAt, c.UpdatedAt
                ORDER BY c.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            parameters.Add("Offset", (paginationParams.PageNumber - 1) * paginationParams.PageSize);
            parameters.Add("PageSize", paginationParams.PageSize);

            var connection = await GetOpenConnectionAsync();

            var totalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await connection.QueryAsync<CategoryResponseDto>(dataSql, parameters);

            return new
            {
                TotalItems = totalItems,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)paginationParams.PageSize),
                Data = items
            };
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

        public async Task<CategoriesStatistics> GetCategoriesStatisticsAsync(int? sellerId = null)
        {
            var whereClause = sellerId.HasValue ? "WHERE c.UserId = @SellerId" : "";
            var sql = $@"
                SELECT 
                    COUNT(c.Id) AS totalCategories,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 0 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'draft' THEN 1 ELSE 0 END) AS Draft,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 1 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'pending' THEN 1 ELSE 0 END) AS Pending,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 2 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'approved' THEN 1 ELSE 0 END) AS Approved,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 3 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'rejected' THEN 1 ELSE 0 END) AS Rejected,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 4 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'archived' THEN 1 ELSE 0 END) AS Archived,
                    SUM(CASE WHEN TRY_CAST(c.Status AS INT) = 5 OR LOWER(CAST(c.Status AS NVARCHAR(50))) = 'suspended' THEN 1 ELSE 0 END) AS Suspended
                FROM Categories c {whereClause};";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<CategoriesStatistics>(sql, new { SellerId = sellerId }) ?? new CategoriesStatistics();
        }
        /// <summary>
        /// Retrieves categories created by a specific user/seller with pagination and optional status filter.
        /// </summary>
        public async Task<object> GetCategoriesByUserIdPagedAsync(int userId, PaginationParamsDtos paginationParams, CategoriesStatus? status = null, string? searchTerm = null)
        {
            var conditions = new List<string> { "c.UserId = @UserId" };
            var parameters = new DynamicParameters();
            parameters.Add("UserId", userId);

            if (status.HasValue)
            {
                conditions.Add("(c.Status = @StatusValue OR LOWER(CAST(c.Status AS NVARCHAR(50))) = LOWER(@StatusName))");
                parameters.Add("StatusValue", (int)status.Value);
                parameters.Add("StatusName", status.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                conditions.Add("(LOWER(c.Name) LIKE LOWER(@SearchTerm) OR LOWER(c.Description) LIKE LOWER(@SearchTerm))");
                parameters.Add("SearchTerm", $"%{searchTerm.Trim()}%");
            }

            var whereClause = "WHERE " + string.Join(" AND ", conditions);

            // Query to count total matching records for the specific user safely
            var countSql = $"SELECT COUNT(DISTINCT c.Id) FROM Categories c {whereClause};";

            // Query for fetching the actual paged data for the user safely
            var dataSql = $@"
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
                {whereClause}
                GROUP BY 
                    c.Id, c.Name, c.Slug, c.Status, c.Description, 
                    c.ImageUrl, c.UserId, u.FullName, c.CreatedAt, c.UpdatedAt
                ORDER BY c.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            parameters.Add("Offset", (paginationParams.PageNumber - 1) * paginationParams.PageSize);
            parameters.Add("PageSize", paginationParams.PageSize);

            var connection = await GetOpenConnectionAsync();

            var totalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await connection.QueryAsync<CategoryResponseDto>(dataSql, parameters);

            return new
            {
                TotalItems = totalItems,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)paginationParams.PageSize),
                Data = items
            };
        }
    }
}