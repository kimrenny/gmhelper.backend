using MatHelper.BLL.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MatHelper.BLL.Middlewares
{
    public class DuplicateRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, DateTime> _recentRequests = new();

        private readonly TimeSpan _duplicateThreshold = TimeSpan.FromMilliseconds(500); // change to longer time if necessary
        private readonly TimeSpan _logsThreshold = TimeSpan.FromMilliseconds(100);

        public DuplicateRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IClientInfoService clientInfoService)
        {
            context.Request.EnableBuffering();

            var ip = clientInfoService.GetClientIp(context);

            var deviceInfo = clientInfoService.GetDeviceInfo(context);
            var deviceKey = $"{deviceInfo.UserAgent}-{deviceInfo.Platform}";

            string bodyHash = string.Empty;
            if(context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
            {
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                bodyHash = ComputeHash(body);
            }

            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
            var threshold = path.Contains("/logs") ? _logsThreshold : _duplicateThreshold;

            var requestKey = $"{ip}-{deviceKey}-{context.Request.Method}-{context.Request.Path}-{bodyHash}";

            if(_recentRequests.TryGetValue(requestKey, out var lastRequestTime))
            {
                if(DateTime.UtcNow - lastRequestTime < threshold)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Duplicate request detected. Please wait before retrying.");
                    return;
                }
            }

            _recentRequests[requestKey] = DateTime.UtcNow;

            await _next(context);
        }

        private static string ComputeHash(string input) 
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }

    public static class DuplicateRequestMiddlewareExtensions
    {
        public static IApplicationBuilder UseDuplicateRequestMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DuplicateRequestMiddleware>();
        }
    }
}
