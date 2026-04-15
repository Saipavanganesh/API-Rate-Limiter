using RateLimiter.Core.Enums;
using RateLimiter.Core.Models;
using RateLimiter.Middleware;
using RateLimiter.Redis.Configuration;
using RateLimiter.Redis.Implementations;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var rateLimitConfig = new RateLimitConfig
{
   RequestsPerWindow = builder.Configuration.GetValue<int>("RateLimiting:RequestsPerWindow"),
   WindowSizeSeconds = builder.Configuration.GetValue<int>("RateLimiting:WindowSizeSeconds"),
   Strategy = builder.Configuration.GetValue<RateLimitStrategy>("RateLimiting:Strategy")

};  
var redisOptions = new RedisOptions
{
    ConnectionString = builder.Configuration.GetValue<string>("RateLimiting:Redis:ConnectionString") ?? "localhost:6379"
};
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions.ConnectionString));
builder.Services.AddSingleton<RateLimiter.Core.Interfaces.IRateLimiter, RedisRateLimiter>();
builder.Services.AddRateLimiting(rateLimitConfig);
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiting();
app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }