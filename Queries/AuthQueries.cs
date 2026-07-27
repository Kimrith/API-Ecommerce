using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    public class AuthQueries
    {
        private readonly AppDbContext _context;

        public AuthQueries(AppDbContext context)
        {
            _context = context;
        }

        // Helper to get and automatically open connection if it's closed
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
        /// Retrieves a single user/auth record by ID using Raw SQL.
        /// </summary>
        public async Task<AuthResponseDto?> GetByIdAsync(long id)
        {
            const string sql = @"
                SELECT 
                    Id AS UserId,
                    FullName,
                    Email,
                    Role,
                    ShopName,
                    Status,
                    ProfileImageUrl
                FROM Auths
                WHERE Id = @Id;";

            using var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<AuthResponseDto>(sql, new { Id = id });
        }

        /// <summary>
        /// Retrieves a user by Email using Raw SQL.
        /// </summary>
        public async Task<AuthResponseDto?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT 
                    Id AS UserId,
                    FullName,
                    Email,
                    Role,
                    ShopName,
                    Status,
                    ProfileImageUrl
                FROM Auths
                WHERE LOWER(Email) = LOWER(@Email);";

            using var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<AuthResponseDto>(sql, new { Email = email });
        }

        /// <summary>
        /// Retrieves all sellers using Raw SQL.
        /// </summary>
        public async Task<IEnumerable<AuthResponseDto>> GetAllSellersAsync()
        {
            const string sql = @"
                SELECT 
                    Id AS UserId,
                    FullName,
                    Email,
                    Role,
                    ShopName,
                    Status,
                    ProfileImageUrl
                FROM Auths
                WHERE CAST(Role AS NVARCHAR(50)) IN ('Seller', '1')
                ORDER BY CreatedAt DESC;";

            using var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<AuthResponseDto>(sql);
        }

        /// <summary>
        /// Retrieves all users with optional status filtering using Raw SQL.
        /// </summary>
        public async Task<IEnumerable<AuthResponseDto>> GetAllUsersAsync(AuthStatus? status = null)
        {
            var sql = @"
                SELECT 
                    Id AS UserId,
                    FullName,
                    Email,
                    Role,
                    ShopName,
                    Status,
                    ProfileImageUrl
                FROM Auths";

            object? parameters = null;

            if (status.HasValue)
            {
                // Handles both DB storage types: matching integer ID (e.g. 0) or string name ("Active")
                sql += " WHERE (Status = @StatusValue OR LOWER(CAST(Status AS NVARCHAR(50))) = LOWER(@StatusName))";
                parameters = new
                {
                    StatusValue = (int)status.Value,
                    StatusName = status.Value.ToString()
                };
            }

            sql += " ORDER BY CreatedAt DESC;";

            using var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<AuthResponseDto>(sql, parameters);
        }
    }
}