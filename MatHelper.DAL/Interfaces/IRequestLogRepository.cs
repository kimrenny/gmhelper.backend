using MatHelper.CORE.Models;
using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IRequestLogRepository
    {
        Task LogRequestAsync(
            string method,
            string path,
            string userId,
            string requestBody,
            int statusCode,
            string startTime,
            string endTime,
            double elapsedTime,
            string ipAddress,
            string userAgent,
            string status,
            string requestType);

        Task<CombinedRequestLogDto> GetRequestStatsAsync();
        Task<List<RequestLogDetail>> GetAllRequestLogsAsync();
        IQueryable<RequestLogDetail> GetLogsQuery();
    }
}
