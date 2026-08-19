using System.Security.Cryptography;
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

        // --- Helper to Generate Secure 64-byte Token ---
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // --- 1. REGISTER USER ---
        public async Task<AuthResponseDto> ExecuteRegisterAsync(RegisterDto dto, string? profileImageUrl = null)
        {
            // Block public registration for Admin role
            if (dto.Role == Roles.Admin)
            {
                throw new InvalidOperationException("Admin registration is not allowed.");
            }

            var existingUser = await _context.Auths
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (existingUser != null)
            {
                throw new InvalidOperationException("An account with this email address already exists.");
            }

            string fullName = $"{dto.FirstName} {dto.LastName}".Trim();
            string refreshToken = GenerateRefreshToken();
            DateTime refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            var authUser = new Auth
            {
                FullName = fullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                ShopName = dto.Role == Roles.Seller ? dto.ShopName : null,
                Status = AuthStatus.Active,
                ProfileImageUrl = profileImageUrl,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = refreshTokenExpiry,
                CreatedAt = DateTime.UtcNow
            };

            _context.Auths.Add(authUser);
            await _context.SaveChangesAsync();

            string token = _tokenService.GenerateToken(authUser);

            return new AuthResponseDto
            {
                UserId = authUser.Id,
                FullName = authUser.FullName,
                Email = authUser.Email,
                PhoneNumber = authUser.PhoneNumber,
                Role = authUser.Role.ToString(),
                ShopName = authUser.ShopName,
                Status = authUser.Status,
                ProfileImageUrl = authUser.ProfileImageUrl,
                Token = token,
                RefreshToken = authUser.RefreshToken,
                RefreshTokenExpiryTime = authUser.RefreshTokenExpiryTime
            };
        }

        // --- 2. LOGIN USER ---
        public async Task<AuthResponseDto> ExecuteLoginAsync(LoginDto dto)
        {
            var user = await _context.Auths
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                throw new InvalidOperationException("Invalid email or password.");
            }

            if (user.Status != AuthStatus.Active)
            {
                throw new InvalidOperationException("Your account is inactive. Please contact support.");
            }

            // Assign new 7-day Refresh Token on login
            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            string token = _tokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                ShopName = user.ShopName,
                Status = user.Status,
                ProfileImageUrl = user.ProfileImageUrl,
                Token = token,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime,
                Addresses = user.Addresses.Select(a => new AddressResponseDto
                {
                    Id = a.Id,
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

        // --- 3. REFRESH TOKEN VALIDATION & ROTATION ---
        public async Task<AuthResponseDto> ExecuteRefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Auths
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            // Validate existence and 7-day expiration window
            if (user == null || user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Invalid or expired refresh token.");
            }

            if (user.Status != AuthStatus.Active)
            {
                throw new InvalidOperationException("Your account is inactive.");
            }

            // Rotate refresh token
            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            string newToken = _tokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                ShopName = user.ShopName,
                Status = user.Status,
                ProfileImageUrl = user.ProfileImageUrl,
                Token = newToken,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
            };
        }
    }
}