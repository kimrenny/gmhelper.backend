using MatHelper.DAL.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace MatHelper.BLL.Filters
{
    public class RequestLoggingFilter : IAsyncActionFilter, IFilterMetadata
    {
        private readonly RequestLogRepository _logRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestLoggingFilter(RequestLogRepository logRepository, IHttpContextAccessor httpContextAccessor)
        {
            _logRepository = logRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if(httpContext != null)
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
                var method = httpContext.Request.Method;
                var path = httpContext.Request.Path;

                string? requestBody = null;

                if(method == "PUT" || method == "DELETE")
                {
                    if (httpContext.Request.Body.CanSeek)
                    {
                        var originalPosition = httpContext.Request.Body.Position;

                        httpContext.Request.Body.Seek(0, SeekOrigin.Begin);

                        using (var reader = new StreamReader(httpContext.Request.Body))
                        {
                            requestBody = await reader.ReadToEndAsync();
                        }

                        httpContext.Request.Body.Seek(originalPosition, SeekOrigin.Begin);

                    }
                }

                await _logRepository.LogRequestAsync(method, path, userId, requestBody ?? string.Empty);
            }

            await next();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {

        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }
    }
}
