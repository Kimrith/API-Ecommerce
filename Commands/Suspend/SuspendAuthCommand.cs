using API_Ecommerce.Data;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Suspend
{
    public class SuspendAuthCommand
    {
        private readonly AppDbContext _context;

        public SuspendAuthCommand(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Suspends a user or seller account by ID.
        /// </summary>
        public async Task<Auth> ExecuteAsync(int userId)
        {
            var user = await _context.Auths.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"Account with ID {userId} was not found.");
            }

            // Updated to AuthStatus
            user.Status = AuthStatus.Suspended;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// Optional: Unsuspends/Reactivates a user or seller account by ID.
        /// </summary>
        public async Task<Auth> ExecuteReactivateAsync(int userId)
        {
            var user = await _context.Auths.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"Account with ID {userId} was not found.");
            }

            // Updated to AuthStatus
            user.Status = AuthStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return user;
        }
    }
}