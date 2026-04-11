using RateLimiter.Core.Enums;

namespace RateLimiter.Core.Models;
public class RateLimitResult
{
    public RateLimitStatus Status { get; set; }
    public int RequestsRemaining { get; set; }
    public int RetryAfterSeconds { get; set; }
}
