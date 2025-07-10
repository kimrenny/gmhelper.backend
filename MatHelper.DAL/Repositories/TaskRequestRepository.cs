using MatHelper.DAL.Database;
using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.DAL.Repositories
{
    public class TaskRequestRepository
    {
        private readonly AppDbContext _context;

        public TaskRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskRequestLog?> GetLastRequestByIpAsync(string ip)
        {
            return await _context.TaskRequestLogs
                .Where(x => x.IpAddress == ip)
                .OrderByDescending(x => x.RequestTime)
                .FirstOrDefaultAsync();
        }

        public async Task AddRequestAsync(TaskRequestLog request)
        {
            await _context.TaskRequestLogs.AddAsync(request);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
