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
        private readonly AuthLogRepository _authLogRepository;
        private readonly ErrorLogRepository _errorLogRepository;
        private readonly ILogger _logger;

        public RequestLogService(RequestLogRepository logRepository, AuthLogRepository authLogRepository, ErrorLogRepository errorLogRepository, ILogger<RequestLogService> logger)
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

        public async Task<List<RequestLogDetail>> GetRequestLogs()
        {
            try
            {
                return await _logRepository.GetAllRequestLogsAsync();
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
