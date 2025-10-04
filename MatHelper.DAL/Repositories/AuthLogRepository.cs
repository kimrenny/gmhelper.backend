using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;

namespace MatHelper.DAL.Repositories
{
    public class AuthLogRepository : IAuthLogRepository
    {
        private readonly AppDbContext _context;

        public AuthLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAuthAsync(
            string userId,          
            string ipAddress,
            string userAgent,
            string status,
            string message = "")
        {

            var authLog = new AuthLog
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Status = status,
                Message = message
            };

            await _context.AuthLogs.AddAsync(authLog);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuthLog>> GetAllAuthLogsAsync()
        {
            return await _context.AuthLogs
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }

        public IQueryable<AuthLog> GetLogsQuery()
        {
            return _context.AuthLogs.AsQueryable();
        }
    }
}
