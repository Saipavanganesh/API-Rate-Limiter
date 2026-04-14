namespace RateLimiter.Redis.Configuration;
public class RedisOptions
{
    public const string SectionName = "RateLimiting:Redis";
    public string ConnectionString { get; set; } = "localhost:6379";
}