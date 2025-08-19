using MatHelper.DAL.Database;
using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using Microsoft.EntityFrameworkCore;
using MatHelper.CORE.Enums;

namespace MatHelper.DAL.Repositories
{
    public class TaskRequestRepository
    {
        private readonly AppDbContext _context;

        public TaskRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskRequestLog?> GetLastRequestByIpAsync(string ip, SubjectType subject)
        {
            return await _context.TaskRequestLogs
                .Where(x => x.IpAddress == ip && x.Subject == subject.ToString())
                .OrderByDescending(x => x.RequestTime)
                .FirstOrDefaultAsync();
        }

        public async Task<TaskRequestLog?> GetRequestByTaskIdAsync(string taskId)
        {
            return await _context.TaskRequestLogs
                .Where(x => x.TaskId == taskId)
                .OrderByDescending(x => x.RequestTime)
                .FirstOrDefaultAsync();
        }

        public async Task AddRequestAsync(TaskRequestLog request)
        {
            await _context.TaskRequestLogs.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
