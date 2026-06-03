using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Interfaces;

namespace MatHelper.DAL.Repositories
{
    public class PasswordRecoveryRepository : IPasswordRecoveryRepository
    {
        private readonly AppDbContext _context;

        public PasswordRecoveryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddPasswordRecoveryTokenAsync(PasswordRecoveryToken recoveryToken)
        {
            await _context.PasswordRecoveryTokens.AddAsync(recoveryToken);
            await _context.SaveChangesAsync();
        }

        public async Task<(RecoverPasswordResult Result, User? User)> GetUserByRecoveryToken(string token)
        {
            var recoveryToken = await _context.PasswordRecoveryTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (recoveryToken == null)
            {
                return (RecoverPasswordResult.TokenNotFound, null);
            }

            if (recoveryToken.IsUsed)
            {
                return (RecoverPasswordResult.TokenUsed, null);
            }

            if (recoveryToken.ExpirationDate <= DateTime.UtcNow)
            {
                return (RecoverPasswordResult.TokenExpired, null);
            }

            recoveryToken.IsUsed = true;
            await _context.SaveChangesAsync();

            return (RecoverPasswordResult.Success, recoveryToken.User);
        }

        public async Task InvalidateAllUserRecoveryTokensAsync(Guid userId)
        {
            var tokens = await _context.PasswordRecoveryTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsUsed = true;

            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
