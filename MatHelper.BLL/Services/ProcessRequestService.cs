using Microsoft.Extensions.Logging;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Http;
using MatHelper.BLL.Interfaces;
using System.Net;

namespace MatHelper.BLL.Services
{
    public class ProcessRequestService : IProcessRequestService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ProcessRequestService> _logger;

        public ProcessRequestService(IHttpContextAccessor httpContextAccessor, ILogger<ProcessRequestService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public (DeviceInfo, string?) GetRequestInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null, returning default device info.");
                return (new DeviceInfo { UserAgent = "Unknown", Platform = "Unknown" }, null);
            }

            var userAgent = httpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            var platform = httpContext.Request.Headers["Sec-CH-UA-Platform"].ToString() ?? "Unknown";

            string? ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.ToString().Split(',')[0].Trim();
            }
            else if (httpContext.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                ipAddress = realIp.ToString();
            }

            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }
            else if (IPAddress.TryParse(ipAddress, out var ip) && ip.IsIPv4MappedToIPv6)
            {
                ipAddress = ip.MapToIPv4().ToString();
            }

            _logger.LogInformation("Extracted device info: UserAgent={UserAgent}, Platform={Platform}, IP={IPAddress}",
            userAgent, platform, ipAddress);

            return (new DeviceInfo { UserAgent = userAgent, Platform = platform }, ipAddress);
        }
    }
}
