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
        /// Retrieves a single user record by ID including Addresses and PhoneNumber using Dapper.
        /// </summary>
        public async Task<AuthResponseDto?> GetByIdAsync(long id)
        {
            const string sql = @"
                SELECT 
                    Id AS UserId,
                    FullName,
                    Email,
                    PhoneNumber,
                    CAST(Role AS NVARCHAR(50)) AS Role,
                    ShopName,
                    Status,
                    ProfileImageUrl
                FROM Auths
                WHERE Id = @Id;

                SELECT 
                    Id,
                    AddressType,
                    StreetAddress,
                    City,
                    State,
                    PostalCode,
                    Country,
                    IsDefault,
                    CreatedAt,
                    UpdatedAt
                FROM Addresses
                WHERE UserId = @Id;";

            var connection = await GetOpenConnectionAsync();
            using var multi = await connection.QueryMultipleAsync(sql, new { Id = id });

            var user = await multi.ReadFirstOrDefaultAsync<AuthResponseDto>();
            if (user != null)
            {
                var addresses = (await multi.ReadAsync<AddressResponseDto>()).ToList();
                user.Addresses = addresses;
            }

            return user;
        }

        /// <summary>
        /// Retrieves a user by Email including Addresses and PhoneNumber using Dapper.
        /// </summary>
        public async Task<AuthResponseDto?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT 
                    Id AS UserId,
                    FullName,
                    Email,
                    PhoneNumber,
                    CAST(Role AS NVARCHAR(50)) AS Role,
                    ShopName,
                    Status,
                    ProfileImageUrl
                FROM Auths
                WHERE LOWER(Email) = LOWER(@Email);

                SELECT 
                    a.Id,
                    a.AddressType,
                    a.StreetAddress,
                    a.City,
                    a.State,
                    a.PostalCode,
                    a.Country,
                    a.IsDefault,
                    a.CreatedAt,
                    a.UpdatedAt
                FROM Addresses a
                INNER JOIN Auths u ON a.UserId = u.Id
                WHERE LOWER(u.Email) = LOWER(@Email);";

            var connection = await GetOpenConnectionAsync();
            using var multi = await connection.QueryMultipleAsync(sql, new { Email = email });

            var user = await multi.ReadFirstOrDefaultAsync<AuthResponseDto>();
            if (user != null)
            {
                var addresses = (await multi.ReadAsync<AddressResponseDto>()).ToList();
                user.Addresses = addresses;
            }

            return user;
        }

        /// <summary>
        /// Retrieves all sellers including their phone number and addresses.
        /// </summary>
        public async Task<IEnumerable<AuthResponseDto>> GetAllSellersAsync()
        {
            const string sql = @"
                SELECT 
                    u.Id AS UserId,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    CAST(u.Role AS NVARCHAR(50)) AS Role,
                    u.ShopName,
                    u.Status,
                    u.ProfileImageUrl,
                    a.Id,
                    a.AddressType,
                    a.StreetAddress,
                    a.City,
                    a.State,
                    a.PostalCode,
                    a.Country,
                    a.IsDefault,
                    a.CreatedAt,
                    a.UpdatedAt
                FROM Auths u
                LEFT JOIN Addresses a ON u.Id = a.UserId
                WHERE CAST(u.Role AS NVARCHAR(50)) IN ('Seller', '1')
                ORDER BY u.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await MapUsersWithAddressesAsync(connection, sql);
        }

        /// <summary>
        /// Retrieves all users with optional status filtering including phone number and addresses.
        /// </summary>
        public async Task<IEnumerable<AuthResponseDto>> GetAllUsersAsync(AuthStatus? status = null)
        {
            var sql = @"
                SELECT 
                    u.Id AS UserId,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    CAST(u.Role AS NVARCHAR(50)) AS Role,
                    u.ShopName,
                    u.Status,
                    u.ProfileImageUrl,
                    a.Id,
                    a.AddressType,
                    a.StreetAddress,
                    a.City,
                    a.State,
                    a.PostalCode,
                    a.Country,
                    a.IsDefault,
                    a.CreatedAt,
                    a.UpdatedAt
                FROM Auths u
                LEFT JOIN Addresses a ON u.Id = a.UserId";

            object? parameters = null;

            if (status.HasValue)
            {
                sql += " WHERE (u.Status = @StatusValue OR LOWER(CAST(u.Status AS NVARCHAR(50))) = LOWER(@StatusName))";
                parameters = new
                {
                    StatusValue = (int)status.Value,
                    StatusName = status.Value.ToString()
                };
            }

            sql += " ORDER BY u.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await MapUsersWithAddressesAsync(connection, sql, parameters);
        }

        private async Task<IEnumerable<AuthResponseDto>> MapUsersWithAddressesAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null)
        {
            var userDictionary = new Dictionary<long, AuthResponseDto>();

            var result = await connection.QueryAsync<AuthResponseDto, AddressResponseDto, AuthResponseDto>(
                sql,
                (user, address) =>
                {
                    if (!userDictionary.TryGetValue(user.UserId, out var currentUser))
                    {
                        currentUser = user;
                        currentUser.Addresses = new List<AddressResponseDto>();
                        userDictionary.Add(currentUser.UserId, currentUser);
                    }

                    if (address != null && address.Id > 0)
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