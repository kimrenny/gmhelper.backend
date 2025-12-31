using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;

namespace MatHelper.DAL.Repositories
{
    public class IpLoginAttemptRepository : IIpLoginAttemptRepository
    {
        private readonly AppDbContext _context;

        public IpLoginAttemptRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<IpLoginAttempt?> GetByIpAsync(string ipAddress)
        {
            return _context.IpLoginAttempts.FirstOrDefaultAsync(x => x.IpAddress == ipAddress);
        }

        public async Task AddAsync(IpLoginAttempt attempt)
        {
            await _context.IpLoginAttempts.AddAsync(attempt);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }

}