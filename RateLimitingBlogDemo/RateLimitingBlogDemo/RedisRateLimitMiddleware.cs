using RateLimitingBlogDemo;
using RateLimitingBlogDemo.Services;
using System.Net;

namespace RedisRateLimitApi.Middleware;

public class RedisRateLimitMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IRedisRateLimiter rateLimiter)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Missing API Key.");
            return;
        }

        if(context.GetEndpoint()?.Metadata.GetMetadata<UseRedisRateLimiterAttribute>() != null)
        {
            bool allowed = await rateLimiter.IsRequestAllowedAsync(apiKey);
            if (!allowed)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded.");
                return;
            }
        }

        await _next(context);
    }
}
