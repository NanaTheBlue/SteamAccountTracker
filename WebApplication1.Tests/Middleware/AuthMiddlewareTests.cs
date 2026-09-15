using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Middleware;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.Tests.Services;
using Xunit;

namespace WebApplication1.Tests.Middleware
{
    public class FakeUserService : IUserService
    {
        public Func<Dtos.RegisterRequest, Task<Dtos.UserDto?>>? RegisterUserHandler { get; set; }
        public Func<Guid, Task<AuthenticatedUser?>>? GetUserFromSessionHandler { get; set; }
        public Func<Dtos.LoginRequest, Task<Dtos.LoginResult>>? LoginUserHandler { get; set; }
        public Func<Guid, Task>? LogoutHandler { get; set; }

        public Task<Dtos.UserDto?> RegisterUser(Dtos.RegisterRequest registerRequest) =>
            RegisterUserHandler != null ? RegisterUserHandler(registerRequest) : Task.FromResult<Dtos.UserDto?>(null);

        public Task<AuthenticatedUser?> GetUserFromSession(Guid id) =>
            GetUserFromSessionHandler != null ? GetUserFromSessionHandler(id) : Task.FromResult<AuthenticatedUser?>(null);

        public Task<Dtos.LoginResult> LoginUser(Dtos.LoginRequest loginRequest) =>
            LoginUserHandler != null ? LoginUserHandler(loginRequest) : Task.FromResult(new Dtos.LoginResult());

        public Task Logout(Guid sessionId) =>
            LogoutHandler != null ? LogoutHandler(sessionId) : Task.CompletedTask;
    }

    public class AuthMiddlewareTests
    {
        private readonly FakeUserService _userService;

        public AuthMiddlewareTests()
        {
            _userService = new FakeUserService();
        }

        [Theory]
        [InlineData("/api/user/login")]
        [InlineData("/api/user/register")]
        [InlineData("/healthz")]
        [InlineData("/swagger")]
        [InlineData("/swagger/index.html")]
        [InlineData("/api/internal/steam/accounts-to-scan")]
        public async Task Invoke_OnPublicOrInternalOrSwaggerPath_PassesThrough(string path)
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new AuthMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = path;

            await middleware.Invoke(context, _userService, NullLogger<AuthMiddleware>.Instance);

            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenMissingSessionCookie_Returns401()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new AuthMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/steam/track";

            await middleware.Invoke(context, _userService, NullLogger<AuthMiddleware>.Instance);

            Assert.False(nextCalled);
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenInvalidGuidCookie_Returns401()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new AuthMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/steam/track";
            context.Request.Headers["Cookie"] = "sessionId=not-a-valid-guid";

            await middleware.Invoke(context, _userService, NullLogger<AuthMiddleware>.Instance);

            Assert.False(nextCalled);
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenSessionExpiredOrNotFound_Returns401()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var sessionId = Guid.NewGuid();
            _userService.GetUserFromSessionHandler = _ => Task.FromResult<AuthenticatedUser?>(null);

            var middleware = new AuthMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/steam/track";
            context.Request.Headers["Cookie"] = $"sessionId={sessionId}";

            await middleware.Invoke(context, _userService, NullLogger<AuthMiddleware>.Instance);

            Assert.False(nextCalled);
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_WhenSessionValid_SetsUserAndCallsNext()
        {
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var sessionId = Guid.NewGuid();
            var expectedUser = new AuthenticatedUser
            {
                Id = Guid.NewGuid(),
                SessionExp = DateTime.UtcNow.AddHours(2)
            };
            _userService.GetUserFromSessionHandler = id => Task.FromResult<AuthenticatedUser?>(expectedUser);

            var middleware = new AuthMiddleware(next);
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/steam/track";
            context.Request.Headers["Cookie"] = $"sessionId={sessionId}";

            await middleware.Invoke(context, _userService, NullLogger<AuthMiddleware>.Instance);

            Assert.True(nextCalled);
            Assert.Same(expectedUser, context.Items["User"]);
        }
    }
}

