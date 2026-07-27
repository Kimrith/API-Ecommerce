using API_Ecommerce.Data;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Delete
{
    public class DeleteAddressCommand
    {
        private readonly AppDbContext _context;

        public DeleteAddressCommand(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExecuteAsync(long addressId, long userId)
        {
            // 1. Retrieve the address ensuring it belongs to the specified user
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
            {
                return false; // Address not found or doesn't belong to the user
            }

            bool wasDefault = address.IsDefault;

            // 2. Remove the address
            _context.Addresses.Remove(address);

            // 3. Smart Default Re-assignment:
            // If the deleted address was the default, make the user's newest remaining address the new default.
            if (wasDefault)
            {
                var nextAddress = await _context.Addresses
                    .Where(a => a.UserId == userId && a.Id != addressId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (nextAddress != null)
                {
                    nextAddress.IsDefault = true;
                    nextAddress.UpdatedAt = DateTime.UtcNow;
                }
            }

            // 4. Save changes to database
            await _context.SaveChangesAsync();
            return true;
        }
    }
}