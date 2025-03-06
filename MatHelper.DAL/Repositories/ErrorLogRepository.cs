using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatHelper.DAL.Models;
using MatHelper.CORE.Models;
using System.Security.Claims;

namespace MatHelper.DAL.Repositories
{
    public class ErrorLogRepository
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
    }
}
