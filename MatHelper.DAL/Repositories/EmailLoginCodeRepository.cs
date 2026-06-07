using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.CORE.Enums;

namespace MatHelper.DAL.Repositories
{
    public class EmailLoginCodeRepository : IEmailLoginCodeRepository
    {
        private readonly AppDbContext _context;

        public EmailLoginCodeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddCodeAsync(EmailLoginCode code)
        {
            await _context.EmailLoginCodes.AddAsync(code);
            await _context.SaveChangesAsync();
        }

        public async Task InvalidateActiveCodesByEmailAsync(string email)
        {
            var activeCodes = await _context.EmailLoginCodes
                .Where(x =>
                    x.Email == email &&
                    !x.IsUsed &&
                    x.Expiration > DateTime.UtcNow)
                .ToListAsync();

            foreach (var code in activeCodes)
            {
                code.IsUsed = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ConfirmTokenResult> ConfirmByEmailAndCodeAsync(string email, string code)
        {
            var emailCode = await _context.EmailLoginCodes
                .FirstOrDefaultAsync(x =>
                    x.Email == email &&
                    x.Code == code);

            if (emailCode == null)
            {
                return ConfirmTokenResult.TokenNotFound;
            }

            if (emailCode.IsUsed)
            {
                return ConfirmTokenResult.TokenUsed;
            }

            if (emailCode.Expiration <= DateTime.UtcNow)
            {
                return ConfirmTokenResult.TokenExpired;
            }

            emailCode.IsUsed = true;

            await _context.SaveChangesAsync();

            return ConfirmTokenResult.Success;
        }

        public async Task<EmailLoginCode?> GetValidCodeAsync(Guid userId, string code)
        {
            return await _context.EmailLoginCodes
                .Where(c => c.UserId == userId && c.Code == code && !c.IsUsed && c.Expiration > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        public async Task<EmailLoginCode?> GetBySessionKeyAsync(string sessionKey)
        {
            return await _context.EmailLoginCodes
                .Where(c => c.SessionKey == sessionKey && !c.IsUsed && c.Expiration > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
