using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;

namespace MatHelper.DAL.Repositories
{
    public class ErrorLogRepository : IErrorLogRepository
    {
        private readonly AppDbContext _context;

        public ErrorLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogErrorAsync(ErrorLog errorLog)
        {
            _context.ErrorLogs.Add(errorLog);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ErrorLog>> GetAllErrorLogsAsync()
        {
            return await _context.ErrorLogs
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }

        public IQueryable<ErrorLog> GetLogsQuery()
        {
            return _context.ErrorLogs.AsQueryable();
        }
    }
}
