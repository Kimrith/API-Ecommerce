using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAdminAsync(AppDbContext context)
        {
            const string adminEmail = "admin@gmail.com";

            // Check if the single admin already exists
            var adminExists = await context.Auths.AnyAsync(u => u.Role == Roles.Admin || u.Email.ToLower() == adminEmail);

            if (!adminExists)
            {
                var adminUser = new Auth
                {
                    FullName = "Super Admin",
                    Email = adminEmail,
                    PhoneNumber = "095248529",
                    // Use a strong default password (change in production)
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@SecurePassword123!"),
                    Role = Roles.Admin,
                    Status = AuthStatus.Active,
                    ShopName = null,
                    CreatedAt = DateTime.UtcNow
                };

                context.Auths.Add(adminUser);
                await context.SaveChangesAsync();
            }
        }
    }
}