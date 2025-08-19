using MatHelper.DAL.Database;
using MatHelper.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.DAL.Repositories
{
    public class TwoFactorRepository
    {
        private readonly AppDbContext _context;

        public TwoFactorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddTwoFactorAsync(UserTwoFactor twoFactor)
        {
            await _context.UserTwoFactors.AddAsync(twoFactor);
            await _context.SaveChangesAsync();
        }

        public async Task<UserTwoFactor?> GetTwoFactorAsync(Guid userId, string type)
        {
            return await _context.UserTwoFactors
                                 .FirstOrDefaultAsync(u => u.UserId == userId && u.Type == type);
        }

        public void Remove(UserTwoFactor twoFactor)
        {
            _context.UserTwoFactors.Remove(twoFactor);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
