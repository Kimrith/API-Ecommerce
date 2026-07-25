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

        // --- 1. REGISTER CUSTOMER / USER ---
        public async Task<AuthResponseDto> ExecuteRegisterAsync(RegisterDto dto, Roles role = Roles.Customer, string? profileImageUrl = null)
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
                Role = role,
                Status = "Active",
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
                Address = authUser.Address,
                ProfileImageUrl = authUser.ProfileImageUrl,
                Token = token
            };
        }

        // --- 2. LOGIN USER ---
        public async Task<AuthResponseDto> ExecuteLoginAsync(LoginDto dto)
        {
            var user = await _context.Auths
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                throw new InvalidOperationException("Invalid email or password.");
            }

            if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
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
                Address = user.Address,
                ProfileImageUrl = user.ProfileImageUrl,
                Token = token
            };
        }

        // --- 3. CREATE SELLER (ADMIN UI OR DIRECT SELLER REGISTRATION) ---
        public async Task<AuthResponseDto> ExecuteCreateSellerAsync(CreateSellerDto dto, string? profileImageUrl = null)
        {
            var existingUser = await _context.Auths
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (existingUser != null)
            {
                throw new InvalidOperationException("An account with this email address already exists.");
            }

            var seller = new Auth
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                ShopName = dto.ShopName,
                Status = dto.Status,
                Address = dto.Address,
                ProfileImageUrl = profileImageUrl,
                Role = Roles.Seller,
                CreatedAt = DateTime.UtcNow
            };

            _context.Auths.Add(seller);
            await _context.SaveChangesAsync();

            // Generate JWT Token for newly created seller
            string token = _tokenService.GenerateToken(seller);

            return new AuthResponseDto
            {
                UserId = seller.Id,
                FullName = seller.FullName,
                Email = seller.Email,
                Role = seller.Role.ToString(),
                ShopName = seller.ShopName,
                Status = seller.Status,
                Address = seller.Address,
                ProfileImageUrl = seller.ProfileImageUrl,
                Token = token
            };
        }
    }
}