using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Create
{
    public class CreateAddressCommand
    {
        private readonly AppDbContext _context;

        public CreateAddressCommand(AppDbContext context)
        {
            _context = context;
        }

        // Changed return type from AddressResponseDto to UserAddressResponseDto
        public async Task<UserAddressResponseDto> ExecuteAsync(long userId, CreateAddressDto dto)
        {
            // 1. Verify that the user exists
            var userExists = await _context.Auths.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                throw new KeyNotFoundException($"User with ID {userId} was not found.");
            }

            // 2. Handle Default Address Logic
            var hasExistingAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId);

            if (!hasExistingAddresses)
            {
                dto.IsDefault = true;
            }
            else if (dto.IsDefault)
            {
                var existingDefaults = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();

                foreach (var existingAddress in existingDefaults)
                {
                    existingAddress.IsDefault = false;
                }
            }

            // 3. Map DTO to Entity
            var address = new Address
            {
                UserId = userId,
                AddressType = dto.AddressType,
                StreetAddress = dto.StreetAddress,
                City = dto.City,
                State = dto.State,
                PostalCode = dto.PostalCode,
                Country = dto.Country,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Save to Database
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            // 5. Return mapped Response DTO
            return new UserAddressResponseDto
            {
                Id = address.Id,
                UserId = address.UserId, // Works seamlessly!
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