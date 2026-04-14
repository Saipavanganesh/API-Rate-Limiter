using Microsoft.AspNetCore.Http;
using RateLimiter.Core.Interfaces;
using RateLimiter.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RateLimiter.Core.Enums;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Metadata;
using System.Xml.Linq;
namespace RateLimiter.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiter _rateLimiter;
    private readonly RateLimitConfig _config;

    //  The Middleware Contract in ASP.NET

  //  Two strict rules ASP.NET requires:

  //1. Constructor must take RequestDelegate next as the first parameter
  //  2. Must have a method named InvokeAsync that takes HttpContext as the first parameter

  //  That's it. Those two things are non-negotiable. Everything else is flexible.
    public RateLimitMiddleware(RequestDelegate next, IRateLimiter rateLimiter, RateLimitConfig config)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _config = config;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _rateLimiter.IsAllowedAsync(key, _config);
        if(result.Status == RateLimitStatus.Exceeded)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = result.RetryAfterSeconds.ToString();
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }
        context.Response.Headers["X-RateLimit-Remaining"] = result.RequestsRemaining.ToString();
        await _next(context);
    }
}
