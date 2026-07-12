using System.Collections.Concurrent;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace MatHelper.BLL.Services;

public sealed class RequestLimitingService : IRequestLimitingService
{
    private readonly RequestLimitingOptions _options;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _ipSemaphores = new();

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _endpointSemaphores = new();

    private readonly SemaphoreSlim _applicationSemaphore;

    public RequestLimitingService(
        IOptions<RequestLimitingOptions> options)
    {
        _options = options.Value;

        _applicationSemaphore = new SemaphoreSlim(
            _options.Default.MaxConcurrentApplication,
            _options.Default.MaxConcurrentApplication);
    }

    public bool TryAcquire(
        HttpContext context,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        var endpointKey = GetEndpointKey(context);

        var rule = GetRule(endpointKey);

        var ip = GetClientIp(context);

        var ipKey = $"{ip}:{endpointKey}";

        var ipSemaphore = _ipSemaphores.GetOrAdd(
            ipKey,
            _ => new SemaphoreSlim(
                rule.MaxConcurrentPerIp,
                rule.MaxConcurrentPerIp));

        if (!ipSemaphore.Wait(0))
        {
            errorMessage =
                "Too many concurrent requests from your IP address.";

            return false;
        }

        var endpointSemaphore = _endpointSemaphores.GetOrAdd(
            endpointKey,
            _ => new SemaphoreSlim(
                rule.MaxConcurrentEndpoint,
                rule.MaxConcurrentEndpoint));

        if (!endpointSemaphore.Wait(0))
        {
            ReleaseIpSemaphore(ipSemaphore);

            errorMessage =
                "This endpoint is currently overloaded. Please try again later.";

            return false;
        }

        if (!_applicationSemaphore.Wait(0))
        {
            endpointSemaphore.Release();

            ReleaseIpSemaphore(ipSemaphore);

            errorMessage =
                "The server is currently under high load. Please try again later.";

            return false;
        }

        context.Items["RateLimit:IpSemaphore"] = ipSemaphore;
        context.Items["RateLimit:EndpointSemaphore"] = endpointSemaphore;

        return true;
    }

    public void Release(HttpContext context)
    {
        if (context.Items.TryGetValue("RateLimit:IpSemaphore", out var ipSemaphoreObj) &&
            ipSemaphoreObj is SemaphoreSlim ipSemaphore)
        {
            ReleaseIpSemaphore(ipSemaphore);
        }

        if (context.Items.TryGetValue("RateLimit:EndpointSemaphore", out var endpointSemaphoreObj) &&
            endpointSemaphoreObj is SemaphoreSlim endpointSemaphore)
        {
            endpointSemaphore.Release();
        }

        _applicationSemaphore.Release();
    }

    private RequestLimitRule GetRule(string endpointKey)
    {
        if (_options.Endpoints.TryGetValue(endpointKey, out var rule))
        {
            return rule;
        }

        return _options.Default;
    }

    private static string GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var value = forwardedFor
                .FirstOrDefault()?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?
                .Trim();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string GetEndpointKey(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint is RouteEndpoint routeEndpoint)
        {
            var rawText = routeEndpoint.RoutePattern.RawText;

            if (!string.IsNullOrWhiteSpace(rawText))
            {
                return $"{context.Request.Method.ToUpperInvariant()}:{rawText}";
            }
        }

        return $"{context.Request.Method.ToUpperInvariant()}:{context.Request.Path}";
    }

    private void ReleaseIpSemaphore(SemaphoreSlim semaphore)
    {
        semaphore.Release();
    }
}