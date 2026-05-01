using MatHelper.DAL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.DAL.Models;

namespace MatHelper.IntegrationTests.Services
{
    public class MockRequestLogRepository : IRequestLogRepository
    {
        public Task LogRequestAsync(string method,
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
            string requestType) => Task.CompletedTask;

        public Task<CombinedRequestLogDto> GetRequestStatsAsync()
            => Task.FromResult(new CombinedRequestLogDto());

        public Task<List<RequestLogDetail>> GetAllRequestLogsAsync()
            => Task.FromResult(new List<RequestLogDetail>());

        public IQueryable<RequestLogDetail> GetLogsQuery()
            => new List<RequestLogDetail>().AsQueryable();
    }
}