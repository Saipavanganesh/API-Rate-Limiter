using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RateLimiter.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RateLimitTestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Request allowed!", timestamp = DateTime.UtcNow });
        }
    }
}
