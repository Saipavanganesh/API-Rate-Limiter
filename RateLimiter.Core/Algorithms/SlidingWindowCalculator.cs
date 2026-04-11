using RateLimiter.Core.Models;

namespace RateLimiter.Core.Algorithms;

public class SlidingWindowCalculator
{
    public int CalculateNewCount(int previousCount, int currentCount, TimeSpan elapsed, TimeSpan windowSize)
    {
        decimal count = previousCount * (1 - (decimal)(elapsed / windowSize)) + currentCount;
        return (int)Math.Ceiling(count);
    }

    public RateLimitResult IsAllowed(int previousCount, RateLimitConfig config, TimeSpan elapsed)
    {
        RateLimitResult rateLimitResult = new();
        int currentCount = 1; //Because we check request by request, so current count is always 1

        int requestsPerWindow = config.RequestsPerWindow;
        int windowSizeSeconds = config.WindowSizeSeconds;

        int newCount = CalculateNewCount(previousCount, currentCount, elapsed, TimeSpan.FromSeconds(windowSizeSeconds));

        bool currentStatus = newCount > requestsPerWindow;

        rateLimitResult.Status = currentStatus ? Enums.RateLimitStatus.Exceeded : Enums.RateLimitStatus.Allowed;
        rateLimitResult.RequestsRemaining = Math.Max(0, (requestsPerWindow - newCount));
        rateLimitResult.RetryAfterSeconds = currentStatus ? windowSizeSeconds : 0;

        return rateLimitResult;
    }
}
