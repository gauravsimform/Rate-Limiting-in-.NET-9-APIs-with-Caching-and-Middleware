namespace RateLimitingBlogDemo.Services
{
    public interface IRedisRateLimiter
    {
        Task<bool> IsRequestAllowedAsync(string key);
    }
}
