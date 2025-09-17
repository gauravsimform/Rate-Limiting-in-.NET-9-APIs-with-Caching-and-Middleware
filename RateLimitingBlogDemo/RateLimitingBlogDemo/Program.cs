
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RateLimitingBlogDemo.Services;
using RedisRateLimitApi.Middleware;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace RateLimitingBlogDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Redis Configuration
            // ===== Add Redis distributed cache =====
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
                options.InstanceName = builder.Configuration["Redis:InstanceName"];
            });

            // Register Redis connection multiplexer
            var redisConnectionString = builder.Configuration["Redis:Connection"];

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnectionString)
            );

            // Register custom rate limiter service
            builder.Services.AddScoped<IRedisRateLimiter, RedisRateLimiter>();
            #endregion

            #region Built-in Rate Limiter Configuration
            // ===== Add built-in rate limiter =====
            builder.Services.AddRateLimiter(options =>
            {
                #region Global Limiter

                // Global limiter: API Key or User ID

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    string partitionKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
                        ?? context.User.FindFirst("sub")?.Value
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";

                    return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 10,
                        TokensPerPeriod = 10,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
                });
                #endregion

                #region Endpoint-specific limiters

                //Endpoint - specific limiters

                // Login attempts
                options.AddFixedWindowLimiter("LoginLimiter", opts =>
                {
                    opts.PermitLimit = 3;                     
                    opts.Window = TimeSpan.FromMinutes(1);  
                    opts.QueueLimit = 0;
                });

                // Weather API calls
                options.AddSlidingWindowLimiter("WeatherLimiter", opts =>
                {
                    opts.PermitLimit = 5;                      // Allow 5 requests
                    opts.Window = TimeSpan.FromMinutes(1);     // Within 1 minute
                    opts.SegmentsPerWindow = 3;                // More granular tracking
                    opts.QueueLimit = 0;
                });
                #endregion

                #region IP-based Rate Limiter

                //Explicit IP-based limiter (Fixed Window Example)
                options.AddPolicy("IPBasedRateLimiter", context =>
                {
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 7,                      // 7 requests
                            Window = TimeSpan.FromMinutes(1),     // per 1 min
                            QueueLimit = 0
                        });
                });

                #endregion

                #region Rate Limiting Algorithms

                //Rate Limiting Algorithms

                //1. Fixed Window Limiter
                options.AddFixedWindowLimiter("FixedWindowLimiter", opts =>
                {
                    opts.PermitLimit = 2;                     // Allow 2 requests
                    opts.Window = TimeSpan.FromMinutes(1);    // Per 1 minute window
                    opts.QueueLimit = 0;                      // Queue Limit 0
                });

                // 2. Sliding Window Limiter
                options.AddSlidingWindowLimiter("SlidingWindowLimiter", opts =>
                {
                    opts.PermitLimit = 4;                      // Allow 4 requests
                    opts.Window = TimeSpan.FromMinutes(1);     // Within 1 minute
                    opts.SegmentsPerWindow = 3;                // 3 Segments Per Window
                    opts.QueueLimit = 0;                       
                });

                // 3. Token Bucket Limiter
                options.AddTokenBucketLimiter("TokenBucketLimiter", opts =>
                {
                    opts.TokenLimit = 1;                        // Max burst
                    opts.TokensPerPeriod = 10;                  // Tokens replenished per period
                    opts.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                    opts.QueueLimit = 0;
                    opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opts.AutoReplenishment = true;
                });

                #endregion

                #region Policy-based Rate Limiting
                // Policy 1: "LoginPolicy"
                options.AddPolicy("CustomPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter("CustomPolicy", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 1,                   // only 3 attempts
                        Window = TimeSpan.FromMinutes(1)   // per minute
                    }));
                #endregion

                #region Custom Rejection Response
                options.RejectionStatusCode = 429; // Too Many Requests

                // Custom rejection response
                options.OnRejected = async (context, token) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        #region Retry-After Header
                        //Relative(seconds)

                        //context.HttpContext.Response.Headers["Retry-After"] =
                        //    ((int)retryAfter.TotalSeconds).ToString();

                        //Absolute (HTTP-date):

                        // Convert retryAfter (TimeSpan) to absolute UTC time
                        var retryAt = DateTime.UtcNow.Add(retryAfter);

                        // Format as RFC1123 ("ddd, dd MMM yyyy HH:mm:ss 'GMT'")
                        context.HttpContext.Response.Headers["Retry-After"] =
                            retryAt.ToString("R"); // "R" = RFC1123 pattern
                        #endregion

                        #region X-Rate-Limit Headers
                        // X-Rate-Limit headers
                        context.HttpContext.Response.Headers["X-Rate-Limit-Limit"] = "10";
                        context.HttpContext.Response.Headers["X-Rate-Limit-Remaining"] = "0";
                        context.HttpContext.Response.Headers["X-Rate-Limit-Reset"] = ((int)retryAfter.TotalSeconds).ToString();
                        #endregion
                    }
                    context.HttpContext.Response.ContentType = "application/json";
                    var response = new
                    {
                        error = "Rate limit exceeded",
                        message = "You have sent too many requests. Please try again later."
                    };
                    var json = JsonSerializer.Serialize(response);
                    await context.HttpContext.Response.WriteAsync(json, token);
                };
                #endregion
            });
            #endregion

            #region JWT Configuration
            // ===== Add JWT authentication =====
            var jwtKey = builder.Configuration["Jwt:Key"];
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                    };
                });

            #endregion

            builder.Services.AddAuthorization();

            // ===== Add Services =====
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<WeatherService>();

            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            #region Swagger Configuration
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Rate Limit API",
                    Version = "v1"
                });

                // 🔐 1. JWT Bearer Token Support
                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Description = "Enter JWT token. Example: Bearer <your_token>",
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });

                // 🔑 2. API Key Support
                var apiKeyScheme = new OpenApiSecurityScheme
                {
                    Description = "Enter your API Key in the request header. Example: X-API-Key: your_api_key",
                    Name = "X-API-Key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyScheme",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKeyScheme"
                    }
                };

                options.AddSecurityDefinition("ApiKeyScheme", apiKeyScheme);

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { apiKeyScheme, Array.Empty<string>() }
                });
            });

            #endregion

            #region CORS Configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            #endregion

            var app = builder.Build();

            app.UseCors("AllowAll");
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rate Limiting API V1");
                    options.RoutePrefix = string.Empty; // Swagger at root URL
                });
                app.MapOpenApi();
            }
            //app.UseMiddleware<RateLimitingMiddleware>();

            // Rate limiting middleware
            app.UseMiddleware<RedisRateLimitMiddleware>();
            
            app.UseRateLimiter();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
