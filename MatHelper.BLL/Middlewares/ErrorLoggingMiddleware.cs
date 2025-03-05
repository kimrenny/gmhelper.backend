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
using System.Net;
using Microsoft.Extensions.Logging;
using MatHelper.DAL.Models;

namespace MatHelper.BLL.Middlewares
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var errorLogRepository = scope.ServiceProvider.GetRequiredService<ErrorLogRepository>();

                    var errorLog = new ErrorLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        Endpoint = context.Request.Path,
                        ExceptionDetails = ex.ToString()
                    };

                    await errorLogRepository.LogErrorAsync(errorLog);
                }
                

                _logger.LogError(ex, "Unhandled exception occured.");

                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("An unexpected error occured.");
            }
        }
    }
}
