using Gateway.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Gateway.Proxy.Middleware;

public class RateLimitingMiddleware(RequestDelegate next, IMemoryCache memoryCache)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Items["MatchedEndpoint"] is ApiEndpoint endpoint && endpoint.EnableRateLimiting)
        {
            var limit = endpoint.RateLimit ?? 100;
            var timeSpanSeconds = endpoint.RatePeriodTimespan ?? 60;

            var clientIdentifier = context.Request.Headers["X-Client-Id"].ToString();
            if (string.IsNullOrEmpty(clientIdentifier))
            {
                clientIdentifier = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }

            var cacheKey = $"ratelimit:{endpoint.Id}:{clientIdentifier}";

            var currentCount = await memoryCache.GetOrCreateAsync(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(timeSpanSeconds);
                return Task.FromResult(0);
            });

            if (currentCount >= limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = timeSpanSeconds.ToString();
                await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
                return;
            }

            memoryCache.Set(cacheKey, currentCount + 1, TimeSpan.FromSeconds(timeSpanSeconds));
        }

        await next(context);
    }
}