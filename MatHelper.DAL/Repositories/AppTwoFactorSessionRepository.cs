using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;

namespace MatHelper.DAL.Repositories
{
    public class AppTwoFactorSessionRepository : IAppTwoFactorSessionRepository
    {
        private readonly AppDbContext _context;

        public AppTwoFactorSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddSessionAsync(AppTwoFactorSession session)
        {
            await _context.TwoFactorSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task<AppTwoFactorSession?> GetBySessionKeyAsync(string sessionKey)
        {
            return await _context.TwoFactorSessions
                .Where(c => c.SessionKey == sessionKey && !c.IsUsed && c.Expiration > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        public async Task<List<AppTwoFactorSession>> GetAllSessionKeysAsync()
        {
            return await _context.TwoFactorSessions.ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
