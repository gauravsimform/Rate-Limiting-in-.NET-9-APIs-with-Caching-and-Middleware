using Microsoft.Extensions.Caching.Memory;
using RateLimitingBlogDemo.Models;

namespace RateLimitingBlogDemo
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly int _limit = 5; // limit of requests
        private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

        public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var cacheKey = $"RateLimit_{ipAddress}";

            var entry = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _period;
                return new RateLimitEntry { Count = 0, StartTime = DateTime.UtcNow };
            });

            if (entry?.Count >= _limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("You have sent too many requests. Please try again later.");
                return;
            }

            entry.Count++;
            _cache.Set(cacheKey, entry);

            await _next(context);
        }
    }
}
