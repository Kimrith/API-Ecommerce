using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Update
{
    public class UpdateAddressCommand
    {
        private readonly AppDbContext _context;

        public UpdateAddressCommand(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AddressResponseDto?> ExecuteAsync(long addressId, long userId, UpdateAddressDto dto)
        {
            // 1. Retrieve the address ensuring it belongs to the specified user
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
            {
                return null; // Return null if address not found or user unauthorized
            }

            // 2. Default Address Management:
            // If updating this address to be Default, unmark all other default addresses for this user
            if (dto.IsDefault && !address.IsDefault)
            {
                var existingDefaults = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault && a.Id != addressId)
                    .ToListAsync();

                foreach (var existingAddress in existingDefaults)
                {
                    existingAddress.IsDefault = false;
                    existingAddress.UpdatedAt = DateTime.UtcNow;
                }
            }

            // 3. Update Address Properties
            address.AddressType = dto.AddressType;
            address.StreetAddress = dto.StreetAddress;
            address.City = dto.City;
            address.State = dto.State;
            address.PostalCode = dto.PostalCode;
            address.Country = dto.Country;
            address.IsDefault = dto.IsDefault;
            address.UpdatedAt = DateTime.UtcNow;

            // 4. Save to Database
            await _context.SaveChangesAsync();

            // 5. Return mapped Response DTO
            return new AddressResponseDto
            {
                Id = address.Id,
                AddressType = address.AddressType,
                StreetAddress = address.StreetAddress,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
        }
    }
}