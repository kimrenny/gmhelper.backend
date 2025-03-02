using MatHelper.DAL.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.DependencyInjection;

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

                if (method == "PUT" || method == "DELETE" || method == "PATCH")
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

                try
                {
                    var executedContext = await next();
                    var endTime = DateTime.UtcNow;
                    var elapsedTime = endTime - startTime;

                    var responseStatusCode = executedContext.HttpContext.Response.StatusCode;

                    if(executedContext.Result is ObjectResult result)
                    {
                        responseStatusCode = result.StatusCode ?? responseStatusCode;
                    }

                    Console.WriteLine(responseStatusCode);
                    string responseStatus = "Info";

                    if (responseStatusCode >= 400 && responseStatusCode < 500)
                    {
                        responseStatus = "Warn";
                    }
                    else if (responseStatusCode >= 500)
                    {
                        responseStatus = "Error";
                    }

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
                        userAgent,
                        responseStatus
                    );
                }
                catch (Exception)
                {
                    var endTime = DateTime.UtcNow;
                    var elapsedTime = endTime - startTime;
                    var responseStatusCode = context.HttpContext.Response.StatusCode;
                    if(responseStatusCode < 400)
                    {
                        responseStatusCode = 500;
                    }

                    var responseStatus = "Error";

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
                        userAgent,
                        responseStatus
                    );

                    throw;
                }
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
