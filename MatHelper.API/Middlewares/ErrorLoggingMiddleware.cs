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
using MatHelper.DAL.Interfaces;

namespace MatHelper.BLL.Middlewares
{
    public class ErrorLoggingMiddleware : IMiddleware
    {
        private readonly ILogger<ErrorLoggingMiddleware> _logger;
        private readonly IErrorLogRepository _errorLogRepository;

        public ErrorLoggingMiddleware(ILogger<ErrorLoggingMiddleware> logger, IErrorLogRepository errorLogRepository)
        {
            _logger = logger;
            _errorLogRepository = errorLogRepository;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var errorLog = new ErrorLog
                {
                    Timestamp = DateTime.UtcNow,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Endpoint = context.Request.Path,
                    ExceptionDetails = ex.ToString()
                };

                await _errorLogRepository.LogErrorAsync(errorLog);

                _logger.LogError(ex, "Unhandled exception occured.");

                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("An unexpected error occured.");
            }
        }
    }
}
