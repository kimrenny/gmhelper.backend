using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatHelper.DAL.Models;
using MatHelper.CORE.Models;

namespace MatHelper.DAL.Repositories
{
    public class RequestLogRepository
    {
        private readonly AppDbContext _context;

        public RequestLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogRequestAsync()
        {
            await IncrementRequestCount();
        }

        public async Task IncrementRequestCount()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var log = await _context.RequestLogs.FirstOrDefaultAsync(x => x.Date == today);

            if(log == null)
            {
                log = new RequestLog { Date = today, Count = 1 };
                await _context.RequestLogs.AddAsync(log);
            }
            else
            {
                log.Count++;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<RequestLogDto>> GetRequestStats()
        {
            return await _context.RequestLogs.OrderByDescending(x => x.Date)
                .Select(group => new RequestLogDto
                {
                    Date = group.Date,
                    Count = (ushort)group.Count
                }).ToListAsync();
        }
    }
}
