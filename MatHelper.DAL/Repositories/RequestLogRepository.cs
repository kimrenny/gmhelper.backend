using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;

namespace MatHelper.DAL.Repositories
{
    public class RequestLogRepository : IRequestLogRepository
    {
        private readonly AppDbContext _context;

        public RequestLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogRequestAsync(
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
            string requestType)
        {
            if(requestType == "Admin")
            {
                await LogAdminRequestAsync();
            }
            else
            {
                await LogRegularRequestAsync();
            }

            await LogRequestDetailsAsync(method, path, userId, requestBody, statusCode, startTime, endTime, elapsedTime, ipAddress, userAgent, status, requestType);
        }

        private async Task LogAdminRequestAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""AdminRequests"" (""Date"", ""Count"")
                VALUES ({0}, 1)
                ON CONFLICT (""Date"") DO UPDATE
                SET ""Count"" = ""AdminRequests"".""Count"" + 1;
            ", today);
        }

        private async Task LogRegularRequestAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""RequestLogs"" (""Date"", ""Count"")
                VALUES ({0}, 1)
                ON CONFLICT (""Date"") DO UPDATE
                SET ""Count"" = ""RequestLogs"".""Count"" + 1;
            ", today);
        }

        private async Task LogRequestDetailsAsync(string method, string path, string userId, string requestBody, int statusCode, string startTime, string endTime, double elapsedTime, string ipAddress, string userAgent, string status, string requestType)
        {
            var requestLogDetail = new RequestLogDetail
            {
                Timestamp = DateTime.UtcNow,
                Method = method,
                Path = path,
                UserId = userId,
                RequestBody = requestBody,
                StatusCode = statusCode,
                StartTime = startTime,
                EndTime = endTime,
                ElapsedTime = elapsedTime,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Status = status,
                RequestType = requestType
            };

            await _context.RequestLogDetails.AddAsync(requestLogDetail);
            await _context.SaveChangesAsync();
        }

        public async Task<CombinedRequestLogDto> GetRequestStatsAsync()
        {
            var requestLogs = await _context.RequestLogs
                .OrderByDescending(x => x.Date)
                .Select(log => new RequestLogDto
                {
                    Date = log.Date,
                    Count = (ushort)log.Count,
                })
                .ToListAsync();

            var adminRequestLogs = await _context.AdminRequests
                .OrderByDescending(x => x.Date)
                .Select(log => new RequestLogDto
                {
                    Date = log.Date,
                    Count = (ushort)log.Count,
                })
                .ToListAsync();

            return new CombinedRequestLogDto
            {
                Regular = requestLogs,
                Admin = adminRequestLogs
            };
        }

        public async Task<List<RequestLogDetail>> GetAllRequestLogsAsync()
        {
            return await _context.RequestLogDetails
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }

        public IQueryable<RequestLogDetail> GetLogsQuery()
        {
            return _context.RequestLogDetails.AsQueryable();
        }
    }
}
