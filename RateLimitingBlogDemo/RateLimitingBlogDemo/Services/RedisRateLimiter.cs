using StackExchange.Redis;

namespace RateLimitingBlogDemo.Services;

public class RedisRateLimiter(IConnectionMultiplexer redis) : IRedisRateLimiter
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<bool> IsRequestAllowedAsync(string key)
    {
        string redisKey = $"rate_limit:{key}";
        var count = await _db.StringIncrementAsync(redisKey);

        if (count == 1)
        {
            int window_Seconds = (int)await _db.StringGetAsync("WINDOW_SECONDS");
            await _db.KeyExpireAsync(redisKey, TimeSpan.FromSeconds(window_Seconds));
        }
        int limit = (int)await _db.StringGetAsync("LIMIT");
        return count <= limit;
    }
}
