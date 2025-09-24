using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface ITaskRequestRepository
    {
        Task<TaskRequestLog?> GetLastRequestByIpAsync(string ip, SubjectType subject);
        Task<TaskRequestLog?> GetRequestByTaskIdAsync(string taskId);
        Task AddRequestAsync(TaskRequestLog request);
        Task SaveChangesAsync();
    }
}
