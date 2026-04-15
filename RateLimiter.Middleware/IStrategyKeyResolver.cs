using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RateLimiter.Middleware;

public interface IStrategyKeyResolver
{
    string Resolve(HttpContext context);
}
