using System.Net;
using System.Text.Json;
using MatHelper.API.Common;
using MatHelper.BLL.Interfaces;

namespace MatHelper.BLL.Middlewares;

public sealed class RequestLimitingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRequestLimitingService requestLimitingService)
    {
        if (!requestLimitingService.TryAcquire(context, out var errorMessage))
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(errorMessage);

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));

            return;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            requestLimitingService.Release(context);
        }
    }
}