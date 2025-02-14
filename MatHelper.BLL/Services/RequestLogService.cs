using MatHelper.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatHelper.DAL.Models;
using MatHelper.BLL.Interfaces;
using Microsoft.Extensions.Logging;
using MatHelper.CORE.Models;

namespace MatHelper.BLL.Services
{
    public class RequestLogService : IRequestLogService
    {
        private readonly RequestLogRepository _logRepository;
        private readonly ILogger _logger;

        public RequestLogService(RequestLogRepository logRepository, ILogger<RequestLogService> logger)
        {
            _logRepository = logRepository;
            _logger = logger;
        }

        public async Task<List<RequestLogDto>> GetRequestStats()
        {
            try
            {
                return await _logRepository.GetRequestStats();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured during get requests stats: {ex}", ex);
                throw new Exception("Error occured during get requests stats.");
            }
            
        }
    }
}
