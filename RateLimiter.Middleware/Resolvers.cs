using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RateLimiter.Middleware;

public class  IpKeyResolver : IStrategyKeyResolver
{
    public string Resolve(HttpContext context) => $"rate_limit:ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

public class UserKeyResolver : IStrategyKeyResolver
{
    public string Resolve(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value ?? "anonymous";
        return $"rate_limit:user:{userId}";
    }
}
public class ApiKeyResolver : IStrategyKeyResolver
{
    public string Resolve(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault() ?? "unknown";
        return $"rate_limit:apiKey:{apiKey}";
    }
}
