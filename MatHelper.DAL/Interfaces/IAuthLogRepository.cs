using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IAuthLogRepository
    {
        Task LogAuthAsync(
            string userId,
            string ipAddress,
            string userAgent,
            string status,
            string message = "");

        Task<List<AuthLog>> GetAllAuthLogsAsync();
    }
}
