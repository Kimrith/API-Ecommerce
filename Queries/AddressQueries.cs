using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Queries
{
    public class AddressQueries
    {
        private readonly AppDbContext _context;

        public AddressQueries(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all addresses for a user using Raw T-SQL, ordered with default address first.
        /// </summary>
        public async Task<List<AddressResponseDto>> GetAddressesByUserIdAsync(long userId)
        {
            // Execute raw SELECT using string interpolation for SQL injection safety
            var items = await _context.Addresses
                .FromSqlInterpolated($@"
                    SELECT 
                        Id, 
                        UserId, 
                        AddressType, 
                        StreetAddress, 
                        City, 
                        State, 
                        PostalCode, 
                        Country, 
                        IsDefault, 
                        CreatedAt, 
                        UpdatedAt 
                    FROM dbo.Addresses 
                    WHERE UserId = {userId} 
                    ORDER BY IsDefault DESC, CreatedAt DESC")
                .AsNoTracking()
                .ToListAsync();

            // Map EF Entity models to DTOs
            return items.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Retrieves a specific address by ID using Raw T-SQL.
        /// </summary>
        public async Task<AddressResponseDto?> GetAddressByIdAsync(long addressId, long userId)
        {
            var item = await _context.Addresses
                .FromSqlInterpolated($@"
                    SELECT TOP (1) 
                        Id, 
                        UserId, 
                        AddressType, 
                        StreetAddress, 
                        City, 
                        State, 
                        PostalCode, 
                        Country, 
                        IsDefault, 
                        CreatedAt, 
                        UpdatedAt 
                    FROM dbo.Addresses 
                    WHERE Id = {addressId} AND UserId = {userId}")
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return item == null ? null : MapToDto(item);
        }

        /// <summary>
        /// Retrieves the default address for a user using Raw T-SQL.
        /// </summary>
        public async Task<AddressResponseDto?> GetDefaultAddressByUserIdAsync(long userId)
        {
            var item = await _context.Addresses
                .FromSqlInterpolated($@"
                    SELECT TOP (1) 
                        Id, 
                        UserId, 
                        AddressType, 
                        StreetAddress, 
                        City, 
                        State, 
                        PostalCode, 
                        Country, 
                        IsDefault, 
                        CreatedAt, 
                        UpdatedAt 
                    FROM dbo.Addresses 
                    WHERE UserId = {userId} AND IsDefault = 1")
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return item == null ? null : MapToDto(item);
        }

        private static AddressResponseDto MapToDto(Models.Address a)
        {
            return new AddressResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                AddressType = a.AddressType,
                StreetAddress = a.StreetAddress,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                Country = a.Country,
                IsDefault = a.IsDefault,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            };
        }
    }
}