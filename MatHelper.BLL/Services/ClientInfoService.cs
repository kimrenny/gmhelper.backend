using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace MatHelper.BLL.Services
{
    public class ClientInfoService : IClientInfoService
    {
        private readonly ILogger _logger;

        public ClientInfoService(ILogger<IClientInfoService> logger)
        {
            _logger = logger;
        }

        public string? GetClientIp(HttpContext context)
        {
            string? ipAddress = null;

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.ToString().Split(',')[0].Trim();
            }
            else if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                ipAddress = realIp.ToString();
            }
            else
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }
            else if (IPAddress.TryParse(ipAddress, out var ip) && ip.IsIPv4MappedToIPv6)
            {
                ipAddress = ip.MapToIPv4().ToString();
            }

            return ipAddress;
        }

        public DeviceInfo GetDeviceInfo(HttpContext context)
        {
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
            var platform = context.Request.Headers["Platform"].FirstOrDefault() ?? "unknown";

            return new DeviceInfo
            {
                UserAgent = userAgent,
                Platform = platform,
            };
        }

        public (DeviceInfo, string?) GetRequestInfo(HttpContext context)
        {
            var deviceInfo = GetDeviceInfo(context);
            var ipAddress = GetClientIp(context);

            return (deviceInfo, ipAddress);
        }
    }
}