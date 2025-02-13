using MatHelper.DAL.Repositories;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatHelper.BLL.Filters
{
    public class RequestLoggingFilter : IAsyncActionFilter, IFilterMetadata
    {
        private readonly RequestLogRepository _logRepository;

        public RequestLoggingFilter(RequestLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await _logRepository.LogRequestAsync();
            await next();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {

        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _logRepository.IncrementRequestCount().Wait();
        }
    }
}
