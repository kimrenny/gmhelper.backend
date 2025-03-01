using MatHelper.DAL.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.IO;
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
            if (httpContext != null)
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
                var method = httpContext.Request.Method;
                var path = httpContext.Request.Path;
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

                var startTime = DateTime.UtcNow;

                string? requestBody = null;

                if (method == "PUT" || method == "DELETE")
                {
                    if (httpContext.Request.Body.CanSeek)
                    {
                        httpContext.Request.EnableBuffering();

                        var originalPosition = httpContext.Request.Body.Position;

                        httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                        {
                            requestBody = await reader.ReadToEndAsync();
                        }

                        httpContext.Request.Body.Seek(originalPosition, SeekOrigin.Begin);
                    }

                    if (!string.IsNullOrEmpty(requestBody))
                    {
                        requestBody = ProcessRequestBody(requestBody);
                    }
                }

                if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
                {
                    ipAddress = forwardedFor.ToString().Split(',')[0].Trim();
                }
                else if(httpContext.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
                {
                    ipAddress = realIp.ToString();
                }

                var executedContext = await next();

                var endTime = DateTime.UtcNow;
                var elapsedTime = endTime - startTime;

                var responseStatusCode = executedContext.HttpContext.Response.StatusCode;

                await _logRepository.LogRequestAsync(
                    method,
                    path,
                    userId,
                    requestBody ?? string.Empty,
                    responseStatusCode,
                    startTime.ToString("HH:mm:ss.fff"),
                    endTime.ToString("HH:mm:ss.fff"),
                    elapsedTime.TotalMilliseconds,
                    ipAddress ?? "Unknown",
                    userAgent
                );
            };
            
        }

        private string ProcessRequestBody(string requestBody)
        {
            var lines = requestBody.Split(new[] { "\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("name=\"currentPassword\"") || lines[i].Contains("name=\"newPassword\""))
                {
                    if (i + 1 < lines.Length)
                    {
                        lines[i + 1] = "******";
                    }
                }
            }
            return string.Join("\n", lines);
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
