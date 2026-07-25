using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Update
{
    public class UpdateAuthCommand
    {
        private readonly AppDbContext _context;

        public UpdateAuthCommand(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Auth> ExecuteAsync(int userId, CreateSellerDto dto, string? newProfileImageUrl = null)
        {
            // 1. Fetch existing Auth record
            var user = await _context.Auths.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} was not found.");
            }

            // 2. Check if Email is being changed to one that is already taken by another account
            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _context.Auths
                    .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != userId);

                if (emailTaken)
                {
                    throw new InvalidOperationException("The email address is already in use by another account.");
                }
            }

            // 3. Update Profile Fields
            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.ShopName = dto.ShopName;
            user.Status = dto.Status;
            user.Address = dto.Address;

            // 4. Update Password safely if provided
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                if (dto.Password != dto.ConfirmPassword)
                {
                    throw new InvalidOperationException("Passwords do not match.");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            // 5. Update Profile Image URL if a new file path is provided
            if (!string.IsNullOrEmpty(newProfileImageUrl))
            {
                user.ProfileImageUrl = newProfileImageUrl;
            }

            user.UpdatedAt = DateTime.UtcNow;

            // 6. Save changes
            _context.Auths.Update(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}