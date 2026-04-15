using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RateLimiter.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RateLimiter.Core.Enums;
namespace RateLimiter.Middleware;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, RateLimitConfig config)
    {
        services.AddSingleton(config);
        IStrategyKeyResolver resolver = config.Strategy switch
        {
            RateLimitStrategy.PerUser => new UserKeyResolver(),
            RateLimitStrategy.PerApiKey => new ApiKeyResolver(),
            _ => new IpKeyResolver(),
        };
        services.AddSingleton<IStrategyKeyResolver>(resolver);
        return services;
    }
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitMiddleware>();
    }
}
