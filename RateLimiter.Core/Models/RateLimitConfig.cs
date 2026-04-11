namespace RateLimiter.Core.Models;

public class RateLimitConfig
{
    public int RequestsPerWindow { get; set; }
    public int WindowSizeSeconds { get; set; }
}