using RateLimiter.Core.Enums;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RateLimiter.Redis.Fallbacks;

public class InMemoryRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, WindowCounter> _counters = new();
    public Task<RateLimitResult> IsAllowedAsync(string key, RateLimitConfig config)
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = config.WindowSizeSeconds * 1000L;
        var currentWindowStart = (nowMs / windowMs) * windowMs;

        var windowKey = $"{key}:{currentWindowStart}";
        var counter = _counters.GetOrAdd(windowKey, _ => new WindowCounter(currentWindowStart + windowMs));
        lock (counter)
        {
            if(counter.Count >= config.RequestsPerWindow)
            {
                var retryAfterSeconds = (int)((counter.ExpiresAt - nowMs) / 1000);
                return Task.FromResult(new RateLimitResult
                {
                    Status = RateLimitStatus.Exceeded,
                    RequestsRemaining = 0,
                    RetryAfterSeconds = retryAfterSeconds > 0 ? retryAfterSeconds : 0
                });
            }
            counter.Count++;
            return Task.FromResult(new RateLimitResult
            {
                Status = RateLimitStatus.Allowed,
                RequestsRemaining = config.RequestsPerWindow - counter.Count,
                RetryAfterSeconds = 0
            });
        }
    }

    private class WindowCounter(long expiresAt)
    {
        public int Count { get;  set; }
        public long ExpiresAt { get; } = expiresAt;
    }
}
