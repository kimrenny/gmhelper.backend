using MatHelper.DAL.Database;
using MatHelper.DAL.Models;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Interfaces;

namespace MatHelper.DAL.Repositories
{
    public class TwoFactorRepository : ITwoFactorRepository
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

        public async Task UpdateTwoFactorModeAsync(Guid userId, string type, bool alwaysAsk)
        {
            var twoFactor = await GetTwoFactorAsync(userId, type);

            if (twoFactor == null) throw new InvalidOperationException("Two-factor authentication not found for this user.");

            if (!twoFactor.IsEnabled)
                throw new InvalidOperationException("Two-factor authentication is not enabled.");

            twoFactor.AlwaysAsk = alwaysAsk;
            twoFactor.UpdatedAt = DateTime.UtcNow;

            await SaveChangesAsync();
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
