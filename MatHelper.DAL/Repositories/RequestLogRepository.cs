using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatHelper.DAL.Models;
using MatHelper.CORE.Models;
using System.Security.Claims;

namespace MatHelper.DAL.Repositories
{
    public class RequestLogRepository
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
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if(requestType == "Admin")
            {
                var log = await _context.AdminRequests.FirstOrDefaultAsync(x => x.Date == today);

                if(log == null)
                {
                    log = new AdminRequestLog { Date = today, Count = 1 };
                    await _context.AdminRequests.AddAsync(log);
                }
                else
                {
                    log.Count++;
                    _context.AdminRequests.Update(log);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                var log = await _context.RequestLogs.FirstOrDefaultAsync(x => x.Date == today);

                if (log == null)
                {
                    log = new RequestLog { Date = today, Count = 1 };
                    await _context.RequestLogs.AddAsync(log);
                }
                else
                {
                    log.Count++;
                    _context.RequestLogs.Update(log);
                }

                await _context.SaveChangesAsync();
            }

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

        public async Task<List<RequestLogDto>> GetRequestStatsAsync()
        {
            return await _context.RequestLogs.OrderByDescending(x => x.Date)
                .Select(group => new RequestLogDto
                {
                    Date = group.Date,
                    Count = (ushort)group.Count
                }).ToListAsync();
        }

        public async Task<List<RequestLogDetail>> GetAllRequestLogsAsync()
        {
            return await _context.RequestLogDetails
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }
    }
}
