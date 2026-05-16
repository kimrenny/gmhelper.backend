using MatHelper.BLL.Interfaces;
using System.Net;
using Microsoft.Extensions.Logging;
using MatHelper.DAL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.BLL.Services
{
    public class ReportService : IReportService
    {
        private readonly INotFoundReportRepository _notFoundRepository;
        private readonly ICacheService _cache;
        private readonly ILogger _logger;

        private const string CacheVersionKey = "notfoundreports:version";

        public ReportService(INotFoundReportRepository notFoundRepository, ICacheService cache, ILogger<ReportService> logger)
        {
            _notFoundRepository = notFoundRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task SubmitNotFoundReportAsync(NotFoundReportRequest request)
        {
            try
            {
                var entity = new NotFoundReport
                {
                    Report = request.Report,
                    Url = request.ClientInfo?.Url ?? string.Empty,
                    UserAgent = request.ClientInfo?.UserAgent,
                    Referrer = request.ClientInfo?.Referrer,
                    Language = request.ClientInfo?.Language,
                    ScreenWidth = request.ClientInfo?.Screen?.Width,
                    ScreenHeight = request.ClientInfo?.Screen?.Height,
                    ViewportWidth = request.ClientInfo?.Viewport?.Width,
                    ViewportHeight = request.ClientInfo?.Viewport?.Height,
                    ClientTimestamp = request.ClientInfo?.Timestamp,
                    IsResolved = false
                };

                await _notFoundRepository.SaveReportAsync(entity);

                var version = await _cache.GetAsync<int?>(CacheVersionKey) ?? 1;
                await _cache.SetAsync(CacheVersionKey, version + 1, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while submitting notfound report.");
                throw new InvalidOperationException("Could not submit notfound report.", ex);
            }

        }

        public async Task<PagedResult<NotFoundReport>> GetNotFoundReportsAsync(int page, int pageSize, string sortBy, bool descending)
        {
            try
            {
                sortBy = string.IsNullOrWhiteSpace(sortBy)
                    ? "Id"
                    : char.ToUpper(sortBy[0]) + sortBy.Substring(1);

                var version = await _cache.GetAsync<int?>(CacheVersionKey) ?? 1;

                var cacheKey = $"notfoundreports:v{version}:{page}:{pageSize}:{sortBy}:{descending}";

                var cached = await _cache.GetAsync<PagedResult<NotFoundReport>>(cacheKey);
                if (cached != null)
                    return cached;

                var query = _notFoundRepository.GetReportsQuery();

                int totalCount = await query.CountAsync();

                query = (sortBy, descending) switch
                {
                    ("Id", false) => query.OrderBy(x => x.Id),
                    ("Id", true) => query.OrderByDescending(x => x.Id),
                    ("Url", false) => query.OrderBy(x => x.Url),
                    ("Url", true) => query.OrderByDescending(x => x.Url),
                    ("ClientTimestamp", false) => query.OrderBy(x => x.ClientTimestamp),
                    ("ClientTimestamp", true) => query.OrderByDescending(x => x.ClientTimestamp),
                    ("IsResolved", false) => query.OrderBy(x => x.IsResolved),
                    ("IsResolved", true) => query.OrderByDescending(x => x.IsResolved),
                    _ => query.OrderBy(x => x.Id)
                };

                var reports = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (reports == null || reports.Count == 0)
                {
                    _logger.LogWarning("No notfound reports found in the database.");
                    throw new InvalidOperationException("No notfound reports found.");
                }

                var result = new PagedResult<NotFoundReport>
                {
                    Items = reports,
                    TotalCount = totalCount
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching notfound reports.");
                throw new InvalidOperationException("Could not fetch notfound reports.", ex);
            }
        }

        public async Task ActionReportAsync(int reportId, string action)
        {
            try
            {
                if (!Enum.TryParse<ReportAction>(action, true, out var parsedAction))
                {
                    throw new ArgumentException("Invalid report action.");
                }

                await _notFoundRepository.ActionReportAsync(reportId, parsedAction);

                var version = await _cache.GetAsync<int?>(CacheVersionKey) ?? 1;
                await _cache.SetAsync(CacheVersionKey, version + 1, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during report action.");
                throw new InvalidOperationException("Could not process report action.", ex);
            }
        }
    }
}