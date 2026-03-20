using MatHelper.BLL.Interfaces;
using System.Net;
using Microsoft.Extensions.Logging;
using MatHelper.DAL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.DAL.Models;

namespace MatHelper.BLL.Services
{
    public class ReportService : IReportService
    {
        private readonly INotFoundReportRepository _notFoundRepository;
        private readonly ILogger _logger;

        public ReportService(INotFoundReportRepository notFoundRepository, ILogger<ReportService> logger)
        {
            _notFoundRepository = notFoundRepository;
            _logger = logger;
        }

        public async Task SubmitNotFoundReportAsync(NotFoundReportRequest request)
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
        }
    }
}