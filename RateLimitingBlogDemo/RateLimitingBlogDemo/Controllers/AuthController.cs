using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RateLimitingBlogDemo.Services;

namespace RateLimitingBlogDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginLimiter")] // Apply per-endpoint limiter
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var token = _authService.GenerateJwtToken(request.Username);
            return Ok(new { token });
        }
    }

    public record LoginRequest(string Username);
}
