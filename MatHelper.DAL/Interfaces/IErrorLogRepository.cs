using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IErrorLogRepository
    {
        Task LogErrorAsync(ErrorLog errorLog);
        Task<List<ErrorLog>> GetAllErrorLogsAsync();
    }
}
