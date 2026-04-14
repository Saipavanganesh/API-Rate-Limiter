using RateLimiter.Core.Enums;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RateLimiter.Redis.Fallbacks;

namespace RateLimiter.Redis.Implementations;
public class RedisRateLimiter : IRateLimiter
{
    private readonly IDatabase _db;
    private readonly InMemoryRateLimiter _fallback;
    private readonly string _luaScript;

    public RedisRateLimiter(IConnectionMultiplexer multiplexer)
    {
        _db = multiplexer.GetDatabase();
        _fallback = new InMemoryRateLimiter();
        _luaScript = LoadLuaScript();
    }

    public async Task<RateLimitResult> IsAllowedAsync(string key, RateLimitConfig config)
    {
        try
        {
            return await ExecuteRedisAsync(key, config);
        }
        catch(RedisException)
        {
            return await _fallback.IsAllowedAsync(key, config);
        }
    }   

    private async Task<RateLimitResult> ExecuteRedisAsync(string key, RateLimitConfig config)
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = config.WindowSizeSeconds * 1000L;
        var currentWindowStart = (nowMs / windowMs) * windowMs;
        var elapsedMs = nowMs - currentWindowStart;

        var currentKey = $"rl:{key}:{currentWindowStart}";
        var previousKey = $"rl:{key}:{currentWindowStart - windowMs}";
        var ttlSeconds = config.WindowSizeSeconds * 2;

        var redisResult = (RedisResult[])await _db.ScriptEvaluateAsync(_luaScript, new RedisKey[] { currentKey, previousKey }, new RedisValue[] { config.RequestsPerWindow, elapsedMs, windowMs, ttlSeconds });
        var result = (RedisResult[])redisResult!;

        bool allowed = (int)result[0] == 1;
        int remaining = (int)result[1];
        int retryAfter = (int)result[2];

        return new RateLimitResult
        {
            Status = allowed ? RateLimitStatus.Allowed : RateLimitStatus.Exceeded,
            RequestsRemaining = remaining,
            RetryAfterSeconds = retryAfter
        };
    }
    private static string LoadLuaScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "RateLimiter.Redis.sliding-window.lua";
        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Embedded Resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
    
