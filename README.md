# Rate Limiting in.NET 9 APIs with Caching and Middleware

A demo ASP.NET Core project showcasing **custom rate limiting middleware**, including **Redis-backed distributed rate limiting**, for modern .NET applications.

---

## ✨ Features
- Custom in-memory rate limiting middleware using `IMemoryCache`
- Redis-based distributed rate limiting with token bucket / fixed window logic
- Attribute-based rate limiting for controllers/actions
- Example authentication and weather endpoints
- Built-in .NET 9 rate limiting policies
- Easily extensible for real-world scenarios

---

## 🏗️ Project Structure
- `Program.cs` – Main entry point and middleware configuration
- `RateLimitingMiddleware.cs` – In-memory rate limiting middleware
- `RedisRateLimitMiddleware.cs` – Middleware for Redis-backed rate limiting
- `UseRedisRateLimiterAttribute.cs` – Attribute for applying Redis rate limiting to controllers/actions
- `Services/` – Business logic and Redis rate limiter implementation
- `Controllers/` – Example API controllers (Auth, Weather)
- `Models/` – Data models for rate limiting and weather

---

## 📚 Overview
Rate limiting controls how many requests a client can make to your API within a defined time window.  
Example rule: *“A user can make only 5 requests per minute. Additional requests receive HTTP 429 Too Many Requests.”*

**Benefits**
- Prevent brute-force and DDoS attacks  
- Ensure fair usage across clients  
- Reduce backend load and improve reliability

---

## ⚡ Rate Limiting Algorithms
- **Token Bucket** – steady rate with short-burst flexibility  
- **Fixed Window** – simple windowed counting  
- **Sliding Window** – fairer continuous window tracking  

Techniques demonstrated:
- **IP-Based** and **Policy-Based** limits  
- **Global** vs **Endpoint-Specific** rules  
- `Retry-After` and custom headers (`X-Rate-Limit-Limit`, `X-Rate-Limit-Remaining`, `X-Rate-Limit-Reset`)

---

## 🚀 Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Redis](https://redis.io/) (required for Redis rate limiting)

### Running the Application
1. Clone the repository:
   ```sh
   git clone <your-repo-url>
   cd RateLimitingBlogDemo
