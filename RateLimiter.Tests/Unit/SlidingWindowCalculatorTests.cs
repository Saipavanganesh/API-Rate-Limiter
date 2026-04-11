using RateLimiter.Core.Algorithms;
using RateLimiter.Core.Enums;
using RateLimiter.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RateLimiter.Tests.Unit;
public class SlidingWindowCalculatorTests
{
    private readonly SlidingWindowCalculator _calculator = new();
    private readonly RateLimitConfig _config = new()
    {
        RequestsPerWindow = 100,
        WindowSizeSeconds = 60
    };

    [Theory]
    [InlineData(50, 10, RateLimitStatus.Allowed)]      // Test 1: Within limit
    [InlineData(99, 0, RateLimitStatus.Allowed)]      // Test 2: One over limit
    [InlineData(100, 0, RateLimitStatus.Exceeded)]     // Test 3: Exactly at limit
    [InlineData(100, 60, RateLimitStatus.Allowed)]     // Test 4: Window fully expired
    [InlineData(80, 30, RateLimitStatus.Allowed)]      // Test 5: Partial window
    public void IsAllowed_WithVariousInputs_ReturnsExpected(int previousCount, int elapsedSeconds, RateLimitStatus expectedStatus)
    {
        var elapsed = TimeSpan.FromSeconds(elapsedSeconds);
        var result = _calculator.IsAllowed(previousCount, _config, elapsed);
        Assert.Equal(expectedStatus, result.Status);
    }

}
