using Gateway.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Gateway.Proxy.Middleware;

public class ResponseCachingMiddleware(RequestDelegate next, IMemoryCache memoryCache)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (context.Items["MatchedEndpoint"] is not ApiEndpoint endpoint || !endpoint.EnableCaching)
        {
            await next(context);
            return;
        }

        var cacheKey = $"cache:{endpoint.Id}:{context.Request.Path}{context.Request.QueryString}";

        if (memoryCache.TryGetValue(cacheKey, out CachedResponse? cachedResponse) && cachedResponse != null)
        {
            context.Response.StatusCode = cachedResponse.StatusCode;
            context.Response.ContentType = cachedResponse.ContentType;
            await context.Response.Body.WriteAsync(cachedResponse.Body);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBodyMemoryStream = new MemoryStream();
        context.Response.Body = responseBodyMemoryStream;

        await next(context);

        if (context.Response.StatusCode == StatusCodes.Status200OK)
        {
            responseBodyMemoryStream.Seek(0, SeekOrigin.Begin);
            var responseBytes = responseBodyMemoryStream.ToArray();

            var ttl = TimeSpan.FromSeconds(endpoint.CacheTtlSeconds > 0 ? endpoint.CacheTtlSeconds : 60);
            memoryCache.Set(cacheKey, new CachedResponse
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType ?? "application/json",
                Body = responseBytes
            }, ttl);

            responseBodyMemoryStream.Seek(0, SeekOrigin.Begin);
            await responseBodyMemoryStream.CopyToAsync(originalBodyStream);
        }
        else
        {
            responseBodyMemoryStream.Seek(0, SeekOrigin.Begin);
            await responseBodyMemoryStream.CopyToAsync(originalBodyStream);
        }
    }

    private class CachedResponse
    {
        public int StatusCode { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public byte[] Body { get; set; } = [];
    }
}