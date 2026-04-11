using RateLimiter.Core.Models;

namespace RateLimiter.Core.Interfaces;
public interface IRateLimiter
{
    Task<RateLimitResult> IsAllowedAsync(string key, RateLimitConfig config);
}
