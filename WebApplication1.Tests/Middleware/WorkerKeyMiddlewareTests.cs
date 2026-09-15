using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using WebApplication1.Middleware;
using Xunit;

namespace WebApplication1.Tests.Middleware
{
    public class WorkerKeyMiddlewareTests
    {
        private const string TestKey = "test-worker-key-1234567890";
        private readonly IConfiguration _config;

        public WorkerKeyMiddlewareTests()
        {
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "WORKER_KEY", TestKey }
                })
                .Build();
        }

        [Fact]
        public async Task Invoke_OnNonInternalPath_PassesThroughWithoutKey()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new WorkerKeyMiddleware(next, _config);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/steam/track";

            await middleware.Invoke(context);

            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_OnInternalPath_WhenKeyMissing_Returns401()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new WorkerKeyMiddleware(next, _config);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/internal/steam/accounts-to-scan";

            await middleware.Invoke(context);

            Assert.False(nextCalled);
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_OnInternalPath_WhenKeyWrong_Returns401()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new WorkerKeyMiddleware(next, _config);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/internal/steam/accounts-to-scan";
            context.Request.Headers["X-Worker-Key"] = "wrong-key";

            await middleware.Invoke(context);

            Assert.False(nextCalled);
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_OnInternalPath_WhenKeyMatches_CallsNext()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new WorkerKeyMiddleware(next, _config);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/internal/steam/accounts-to-scan";
            context.Request.Headers["X-Worker-Key"] = TestKey;

            await middleware.Invoke(context);

            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }
    }
}

