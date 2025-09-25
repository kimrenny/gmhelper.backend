using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace MatHelper.Tests
{
    public class ClientInfoServiceTests
    {
        private readonly ClientInfoService _service;

        public ClientInfoServiceTests()
        {
            var loggerMock = new Mock<ILogger<IClientInfoService>>();
            _service = new ClientInfoService(loggerMock.Object);
        }

        private HttpContext CreateContextWithHeaders(string? xForwardedFor = null, string? xRealIp = null, string? userAgent = null, string? platform = null, string? remoteIp = null)
        {
            var context = new DefaultHttpContext();
            if (xForwardedFor != null) context.Request.Headers["X-Forwarded-For"] = xForwardedFor;
            if (xRealIp != null) context.Request.Headers["X-Real-IP"] = xRealIp;
            if (userAgent != null) context.Request.Headers["User-Agent"] = userAgent;
            if (platform != null) context.Request.Headers["Platform"] = platform;
            if (remoteIp != null) context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);

            return context;
        }

        [Fact]
        public void GetClientIp_ShouldReturnXForwardedFor_WhenPresent()
        {
            var context = CreateContextWithHeaders(xForwardedFor: "1.2.3.4, 5.6.7.8");
            var ip = _service.GetClientIp(context);
            Assert.Equal("1.2.3.4", ip);
        }

        [Fact]
        public void GetClientIp_ShouldReturnXRealIp_WhenForwardedForAbsent()
        {
            var context = CreateContextWithHeaders(xRealIp: "5.6.7.8");
            var ip = _service.GetClientIp(context);
            Assert.Equal("5.6.7.8", ip);
        }

        [Fact]
        public void GetClientIp_ShouldReturnRemoteIp_WhenHeadersAbsent()
        {
            var context = CreateContextWithHeaders(remoteIp: "9.8.7.6");
            var ip = _service.GetClientIp(context);
            Assert.Equal("9.8.7.6", ip);
        }

        [Fact]
        public void GetClientIp_ShouldConvertLoopbackAndMappedIPv6()
        {
            var context1 = CreateContextWithHeaders(remoteIp: "::1");
            Assert.Equal("127.0.0.1", _service.GetClientIp(context1));

            var context2 = CreateContextWithHeaders(remoteIp: "::ffff:192.168.1.1");
            Assert.Equal("192.168.1.1", _service.GetClientIp(context2));
        }

        [Fact]
        public void GetDeviceInfo_ShouldReturnHeadersOrUnknown()
        {
            var context = CreateContextWithHeaders(userAgent: "ua", platform: "plat");
            var device = _service.GetDeviceInfo(context);
            Assert.Equal("ua", device.UserAgent);
            Assert.Equal("plat", device.Platform);

            var context2 = CreateContextWithHeaders();
            var device2 = _service.GetDeviceInfo(context2);
            Assert.Equal("unknown", device2.UserAgent);
            Assert.Equal("unknown", device2.Platform);
        }

        [Fact]
        public void GetRequestInfo_ShouldReturnTupleCorrectly()
        {
            var context = CreateContextWithHeaders(xForwardedFor: "1.1.1.1", userAgent: "ua", platform: "plat");
            var (device, ip) = _service.GetRequestInfo(context);
            Assert.Equal("1.1.1.1", ip);
            Assert.Equal("ua", device.UserAgent);
            Assert.Equal("plat", device.Platform);
        }
    }
}
