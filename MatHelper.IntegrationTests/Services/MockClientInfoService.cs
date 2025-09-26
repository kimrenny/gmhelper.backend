using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Http;

namespace MatHelper.IntegrationTests.Services
{
    public class MockClientInfoService : IClientInfoService
    {
        public string? GetClientIp(HttpContext context)
        {
            return "127.0.0.1";
        }

        public DeviceInfo GetDeviceInfo(HttpContext context)
        {
            return new DeviceInfo
            {
                Platform = "Test",
                UserAgent = "IntegrationTest"
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
