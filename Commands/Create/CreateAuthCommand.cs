using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using API_Ecommerce.Services;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Create
{
    public class CreateAuthCommand
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;

        public CreateAuthCommand(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // --- 1. REGISTER USER WITH SELECTABLE ROLE ---
        public async Task<AuthResponseDto> ExecuteRegisterAsync(RegisterDto dto, string? profileImageUrl = null)
        {
            var existingUser = await _context.Auths
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (existingUser != null)
            {
                throw new InvalidOperationException("An account with this email address already exists.");
            }

            string fullName = $"{dto.FirstName} {dto.LastName}".Trim();

            var authUser = new Auth
            {
                FullName = fullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role, // Reads chosen role directly from DTO
                ShopName = dto.Role == Roles.Seller ? dto.ShopName : null,
                Status = AuthStatus.Active,
                ProfileImageUrl = profileImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Auths.Add(authUser);
            await _context.SaveChangesAsync();

            // Generate JWT Token upon registration
            string token = _tokenService.GenerateToken(authUser);

            return new AuthResponseDto
            {
                UserId = authUser.Id,
                FullName = authUser.FullName,
                Email = authUser.Email,
                Role = authUser.Role.ToString(),
                ShopName = authUser.ShopName,
                Status = authUser.Status,
                ProfileImageUrl = authUser.ProfileImageUrl,
                Token = token,
                Addresses = new List<AddressResponseDto>() // Freshly registered users have no addresses yet
            };
        }

        // --- 2. LOGIN USER ---
        public async Task<AuthResponseDto> ExecuteLoginAsync(LoginDto dto)
        {
            var user = await _context.Auths
                .Include(u => u.Addresses) // Include addresses for response mapping
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                throw new InvalidOperationException("Invalid email or password.");
            }

            if (user.Status != AuthStatus.Active)
            {
                throw new InvalidOperationException("Your account is inactive. Please contact support.");
            }

            // Generate JWT Token upon login
            string token = _tokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                ShopName = user.ShopName,
                Status = user.Status,
                ProfileImageUrl = user.ProfileImageUrl,
                Token = token,
                Addresses = user.Addresses.Select(a => new AddressResponseDto
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
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }
    }
}