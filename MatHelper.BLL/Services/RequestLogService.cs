using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using MatHelper.CORE.Models;
using MatHelper.BLL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.BLL.Services
{
    public class RequestLogService : IRequestLogService
    {
        private readonly IRequestLogRepository _logRepository;
        private readonly IAuthLogRepository _authLogRepository;
        private readonly IErrorLogRepository _errorLogRepository;
        private readonly ILogger _logger;

        public RequestLogService(IRequestLogRepository logRepository, IAuthLogRepository authLogRepository, IErrorLogRepository errorLogRepository, ILogger<RequestLogService> logger)
        {
            _logRepository = logRepository;
            _authLogRepository = authLogRepository;
            _errorLogRepository = errorLogRepository;
            _logger = logger;
        }

        public async Task<CombinedRequestLogDto> GetRequestStats()
        {
            try
            {
                return await _logRepository.GetRequestStatsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured during get requests stats: {ex}", ex);
                throw new Exception("Error occured during get requests stats.");
            }
        }

        public async Task<PagedResult<RequestLogDetail>> GetRequestLogs(int page, int pageSize, string sortBy, bool descending, DateTime? maxLogDate)
        {
            try
            {
                var logsQuery = _logRepository.GetLogsQuery();

                int totalCount = await logsQuery.CountAsync();

                sortBy = string.IsNullOrWhiteSpace(sortBy) ? "Timestamp" : char.ToUpper(sortBy[0]) + sortBy.Substring(1);

                logsQuery = (sortBy, descending) switch
                {
                    ("Id", false) => logsQuery.OrderBy(l => l.Id),
                    ("Id", true) => logsQuery.OrderByDescending(l => l.Id),

                    ("Timestamp", false) => logsQuery.OrderBy(l => l.Timestamp),
                    ("Timestamp", true) => logsQuery.OrderByDescending(l => l.Timestamp),

                    ("Method", false) => logsQuery.OrderBy(l => l.Method),
                    ("Method", true) => logsQuery.OrderByDescending(l => l.Method),

                    ("Path", false) => logsQuery.OrderBy(l => l.Path),
                    ("Path", true) => logsQuery.OrderByDescending(l => l.Path),

                    ("UserId", false) => logsQuery.OrderBy(l => l.UserId),
                    ("UserId", true) => logsQuery.OrderByDescending(l => l.UserId),

                    ("RequestBody", false) => logsQuery.OrderBy(l => l.RequestBody),
                    ("RequestBody", true) => logsQuery.OrderByDescending(l => l.RequestBody),

                    ("StatusCode", false) => logsQuery.OrderBy(l => l.StatusCode),
                    ("StatusCode", true) => logsQuery.OrderByDescending(l => l.StatusCode),

                    ("StartTime", false) => logsQuery.OrderBy(l => l.StartTime),
                    ("StartTime", true) => logsQuery.OrderByDescending(l => l.StartTime),

                    ("EndTime", false) => logsQuery.OrderBy(l => l.EndTime),
                    ("EndTime", true) => logsQuery.OrderByDescending(l => l.EndTime),

                    ("ElapsedTime", false) => logsQuery.OrderBy(l => l.ElapsedTime),
                    ("ElapsedTime", true) => logsQuery.OrderByDescending(l => l.ElapsedTime),

                    ("IpAddress", false) => logsQuery.OrderBy(l => l.IpAddress),
                    ("IpAddress", true) => logsQuery.OrderByDescending(l => l.IpAddress),

                    ("UserAgent", false) => logsQuery.OrderBy(l => l.UserAgent),
                    ("UserAgent", true) => logsQuery.OrderByDescending(l => l.UserAgent),

                    ("Status", false) => logsQuery.OrderBy(l => l.Status),
                    ("Status", true) => logsQuery.OrderByDescending(l => l.Status),

                    ("RequestType", false) => logsQuery.OrderBy(l => l.RequestType),
                    ("RequestType", true) => logsQuery.OrderByDescending(l => l.RequestType),

                    _ => logsQuery.OrderByDescending(l => l.Timestamp)
                };

                var logs = await logsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if(logs == null || logs.Count == 0)
                {
                    _logger.LogWarning("No logs found in the database.");
                    throw new InvalidOperationException("No logs found.");
                }

                return new PagedResult<RequestLogDetail>
                {
                    Items = logs,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured during get requests: {ex}", ex);
                throw new Exception("Error occured during get requests.");
            }
        }

        public async Task<List<AuthLog>> GetAuthLogs()
        {
            try
            {
                return await _authLogRepository.GetAllAuthLogsAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError("Error occured during get auth logs: {ex}", ex);
                throw new Exception("Error occured during get auth logs.");
            }
        }

        public async Task<List<ErrorLog>> GetErrorLogs()
        {
            try
            {
                return await _errorLogRepository.GetAllErrorLogsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured during get error logs: {ex}", ex);
                throw new Exception("Error occured during get error logs.");
            }
        }
    }
}
