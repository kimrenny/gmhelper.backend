using MatHelper.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatHelper.DAL.Models;
using MatHelper.BLL.Interfaces;

namespace MatHelper.BLL.Services
{
    public class RequestLogService : IRequestLogService
    {
        private readonly RequestLogRepository _logRepository;

        public RequestLogService(RequestLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task<List<RequestLog>> GetRequestStats()
        {
            return await _logRepository.GetRequestStats();
        }
    }
}
