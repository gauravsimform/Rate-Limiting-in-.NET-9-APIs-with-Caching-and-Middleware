using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RateLimitingBlogDemo;
using RateLimitingBlogDemo.Services;

namespace RateLimitDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("InBuiltRateLimiting")]
        [EnableRateLimiting("WeatherLimiter")] // Apply specific limiter
        [Authorize]
        public IEnumerable<WeatherForecast> GetA()
        {
            var weathers = _weatherService.GetForecast();
            return weathers.ToList();
        }

        [HttpGet("RedisRateLimiting")]
        [UseRedisRateLimiter]
        [Authorize]
        public IEnumerable<WeatherForecast> GetB()
        {
            var weathers = _weatherService.GetForecast();
            return weathers.ToList();
        }

        [HttpGet("FixedWindowLimiter")]
        [EnableRateLimiting("FixedWindowLimiter")]
        [Authorize]
        public IActionResult GetC()
        {
            return Ok(new { Message = "This is the example of Fixed Window Limiter" });
        }

        [HttpGet("SlidingWindowLimiter")]
        [EnableRateLimiting("SlidingWindowLimiter")]
        [Authorize]
        public IActionResult GetD()
        {
            return Ok(new { Message = "This is the example of Sliding Window Limiter" });
        }

        [HttpGet("TokenBucketLimiter")]
        [EnableRateLimiting("TokenBucketLimiter")]
        [Authorize]
        public IActionResult GetE()
        {
            return Ok(new { Message = "This is the example of Token Bucket Limiter" });
        }

        [HttpGet("IPBasedRateLimiter")]
        [EnableRateLimiting("IPBasedRateLimiter")]
        [Authorize]
        public IActionResult GetF()
        {
            return Ok(new { Message = "This is the example of IP Based Rate Limiting" });
        }

        [HttpGet("PolicyBasedRateLimiter")]
        [EnableRateLimiting("CustomPolicy")]
        [Authorize]
        public IActionResult GetG()
        {
            return Ok(new { Message = "This is the example of Policy Based Rate Limiting" });
        }


    }
}
