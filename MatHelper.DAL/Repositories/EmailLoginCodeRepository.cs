using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;

namespace MatHelper.DAL.Repositories
{
    public class EmailLoginCodeRepository
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

        public async Task<EmailLoginCode?> GetValidCodeAsync(Guid userId, string code)
        {
            return await _context.EmailLoginCodes
                .Where(c => c.UserId == userId && c.Code == code && !c.IsUsed && c.Expiration > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
