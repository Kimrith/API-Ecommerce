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

        public async Task<List<UserAddressResponseDto>> GetAddressesByUserIdAsync(long userId)
        {
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

            return items.Select(MapToUserAddressDto).ToList();
        }

        public async Task<UserAddressResponseDto?> GetAddressByIdAsync(long addressId, long userId)
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

            return item == null ? null : MapToUserAddressDto(item);
        }

        private static UserAddressResponseDto MapToUserAddressDto(Models.Address a)
        {
            return new UserAddressResponseDto
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