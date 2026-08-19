using System.Data;
using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Queries
{
    public class AuthQueries
    {
        private readonly AppDbContext _context;

        public AuthQueries(AppDbContext context)
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

        public async Task<AuthResponseDto?> GetByIdAsync(long id)
        {
            const string sql = @"
                SELECT 
                    Id AS UserId, FullName, Email, PhoneNumber, 
                    CAST(Role AS NVARCHAR(50)) AS Role, ShopName, Status, 
                    ProfileImageUrl, RefreshToken, RefreshTokenExpiryTime 
                FROM Auths 
                WHERE Id = @Id;

                SELECT 
                    Id, AddressType, StreetAddress, City, State, 
                    PostalCode, Country, IsDefault, CreatedAt, UpdatedAt 
                FROM Addresses 
                WHERE UserId = @Id;";

            var connection = await GetOpenConnectionAsync();
            using var multi = await connection.QueryMultipleAsync(sql, new { Id = id });

            var user = await multi.ReadFirstOrDefaultAsync<AuthResponseDto>();
            if (user != null)
            {
                user.Addresses = (await multi.ReadAsync<AddressResponseDto>()).ToList();
            }

            return user;
        }

        public async Task<AuthResponseDto?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT 
                    Id AS UserId, FullName, Email, PhoneNumber, 
                    CAST(Role AS NVARCHAR(50)) AS Role, ShopName, Status, 
                    ProfileImageUrl, RefreshToken, RefreshTokenExpiryTime 
                FROM Auths 
                WHERE LOWER(Email) = LOWER(@Email);

                SELECT 
                    a.Id, a.AddressType, a.StreetAddress, a.City, a.State, 
                    a.PostalCode, a.Country, a.IsDefault, a.CreatedAt, a.UpdatedAt 
                FROM Addresses a 
                INNER JOIN Auths u ON a.UserId = u.Id 
                WHERE LOWER(u.Email) = LOWER(@Email);";

            var connection = await GetOpenConnectionAsync();
            using var multi = await connection.QueryMultipleAsync(sql, new { Email = email });

            var user = await multi.ReadFirstOrDefaultAsync<AuthResponseDto>();
            if (user != null)
            {
                user.Addresses = (await multi.ReadAsync<AddressResponseDto>()).ToList();
            }

            return user;
        }

        /// <summary>
        /// Retrieves all sellers with pagination.
        /// </summary>
        public async Task<object> GetAllSellersAsync(PaginationParamsDtos paginationParams)
        {
            const string countSql = @"
                SELECT COUNT(1) 
                FROM Auths u 
                WHERE u.Role = @RoleInt OR CAST(u.Role AS NVARCHAR(50)) = @RoleStr;";

            // Paginates the users first, then joins addresses to avoid row-multiplication bug
            const string dataSql = @"
                WITH PagedUsers AS (
                    SELECT 
                        u.Id AS UserId, u.FullName, u.Email, u.PhoneNumber, 
                        CAST(u.Role AS NVARCHAR(50)) AS Role, u.ShopName, 
                        u.Status, u.ProfileImageUrl, u.RefreshToken, 
                        u.RefreshTokenExpiryTime, u.CreatedAt
                    FROM Auths u
                    WHERE u.Role = @RoleInt OR CAST(u.Role AS NVARCHAR(50)) = @RoleStr
                    ORDER BY u.CreatedAt DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                )
                SELECT 
                    p.UserId, p.FullName, p.Email, p.PhoneNumber, p.Role, 
                    p.ShopName, p.Status, p.ProfileImageUrl, p.RefreshToken, 
                    p.RefreshTokenExpiryTime,
                    a.Id, a.AddressType, a.StreetAddress, a.City, a.State, 
                    a.PostalCode, a.Country, a.IsDefault, a.CreatedAt, a.UpdatedAt
                FROM PagedUsers p
                LEFT JOIN Addresses a ON p.UserId = a.UserId
                ORDER BY p.CreatedAt DESC;";

            var parameters = new DynamicParameters();
            parameters.Add("RoleInt", (int)Roles.Seller);
            parameters.Add("RoleStr", Roles.Seller.ToString());
            parameters.Add("Offset", (paginationParams.PageNumber - 1) * paginationParams.PageSize);
            parameters.Add("PageSize", paginationParams.PageSize);

            var connection = await GetOpenConnectionAsync();
            var totalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var users = await MapPagedUsersWithAddressesAsync(connection, dataSql, parameters);

            return new
            {
                TotalItems = totalItems,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)paginationParams.PageSize),
                Data = users
            };
        }

        /// <summary>
        /// Retrieves all users with pagination and optional status filter.
        /// </summary>
        public async Task<object> GetAllUsersAsync(PaginationParamsDtos paginationParams, AuthStatus? status = null)
        {
            var whereClause = string.Empty;
            var parameters = new DynamicParameters();

            if (status.HasValue)
            {
                whereClause = " WHERE (u.Status = @StatusValue OR LOWER(CAST(u.Status AS NVARCHAR(50))) = LOWER(@StatusName))";
                parameters.Add("StatusValue", (int)status.Value);
                parameters.Add("StatusName", status.Value.ToString());
            }

            var countSql = $"SELECT COUNT(1) FROM Auths u{whereClause};";

            var dataSql = $@"
                WITH PagedUsers AS (
                    SELECT 
                        u.Id AS UserId, u.FullName, u.Email, u.PhoneNumber, 
                        CAST(u.Role AS NVARCHAR(50)) AS Role, u.ShopName, 
                        u.Status, u.ProfileImageUrl, u.RefreshToken, 
                        u.RefreshTokenExpiryTime, u.CreatedAt
                    FROM Auths u
                    {whereClause}
                    ORDER BY u.CreatedAt DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                )
                SELECT 
                    p.UserId, p.FullName, p.Email, p.PhoneNumber, p.Role, 
                    p.ShopName, p.Status, p.ProfileImageUrl, p.RefreshToken, 
                    p.RefreshTokenExpiryTime,
                    a.Id, a.AddressType, a.StreetAddress, a.City, a.State, 
                    a.PostalCode, a.Country, a.IsDefault, a.CreatedAt, a.UpdatedAt
                FROM PagedUsers p
                LEFT JOIN Addresses a ON p.UserId = a.UserId
                ORDER BY p.CreatedAt DESC;";

            parameters.Add("Offset", (paginationParams.PageNumber - 1) * paginationParams.PageSize);
            parameters.Add("PageSize", paginationParams.PageSize);

            var connection = await GetOpenConnectionAsync();
            var totalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var users = await MapPagedUsersWithAddressesAsync(connection, dataSql, parameters);

            return new
            {
                TotalItems = totalItems,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)paginationParams.PageSize),
                Data = users
            };
        }

        private static async Task<IEnumerable<AuthResponseDto>> MapPagedUsersWithAddressesAsync(
            IDbConnection connection,
            string sql,
            DynamicParameters parameters)
        {
            var userDictionary = new Dictionary<long, AuthResponseDto>();

            await connection.QueryAsync<AuthResponseDto, AddressResponseDto, AuthResponseDto>(
                sql,
                (user, address) =>
                {
                    if (!userDictionary.TryGetValue(user.UserId, out var currentUser))
                    {
                        currentUser = user;
                        currentUser.Addresses = new List<AddressResponseDto>();
                        userDictionary.Add(currentUser.UserId, currentUser);
                    }

                    if (address != null && address.Id > 0 && !currentUser.Addresses.Any(a => a.Id == address.Id))
                    {
                        currentUser.Addresses.Add(address);
                    }

                    return currentUser;
                },
                parameters,
                splitOn: "Id"
            );

            return userDictionary.Values;
        }
    }
}