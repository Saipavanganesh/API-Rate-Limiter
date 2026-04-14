using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.Redis;

namespace RateLimiter.Tests.Integration
{
    public class RateLimitIntegrationTests : IAsyncLifetime
    {
        private readonly RedisContainer _redisContainer;
        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;

        public RateLimitIntegrationTests()
        {
            _redisContainer = new RedisBuilder().Build();
        }
        public async Task InitializeAsync()
        {
            await _redisContainer.StartAsync();
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host =>
                {
                    host.UseSetting("RateLimiting:RequestsPerWindow", "5");
                    host.UseSetting("RateLimiting:WindowSizeSeconds", "60");
                    host.UseSetting("RateLimiting:Redis:ConnectionString", _redisContainer.GetConnectionString());

                    host.ConfigureServices(services =>
                    {
                        services.RemoveAll<IConnectionMultiplexer>();
                        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
                    });
                });
            _client = _factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
            _factory.Dispose();
            await _redisContainer.DisposeAsync();
        }

        [Fact]
        public async Task RequestsWithinLimit200()
        {
            for(int i = 0; i < 5; i++)
            {
                var response = await _client.GetAsync("/api/RateLimitTest/ping");
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            }
        }
        [Fact]
        public async Task RequestExceedingLimit_Returns429()
        {
            for (int i = 0; i < 5; i++)
                await _client.GetAsync("/api/RateLimitTest/ping");

            var response = await _client.GetAsync("/api/RateLimitTest/ping");
            Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        [Fact]
        public async Task RequestExceedingLimit_HasRetryAfterHeader()
        {
            for (int i = 0; i < 5; i++)
                await _client.GetAsync("/api/RateLimitTest/ping");

            var response = await _client.GetAsync("/api/RateLimitTest/ping");
            Assert.True(response.Headers.Contains("Retry-After"));
        }
    }
}
