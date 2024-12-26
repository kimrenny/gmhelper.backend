using MatHelper.CORE.Models;
using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MatHelper.DAL.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.Include(u => u.LoginTokens).FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _context.Users.Include(u => u.LoginTokens).FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<LoginToken> GetLoginTokenByRefreshTokenAsync(string refreshToken)
        {
            var query = _context.LoginTokens.Where(t => t.RefreshToken == refreshToken);
            return await query.FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}