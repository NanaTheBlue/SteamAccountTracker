using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Controllers;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Repository;
using WebApplication1.Services;
using Xunit;

namespace WebApplication1.Tests.Controllers
{
    public class FakeUserRepository : IUserRepository
    {
        public Func<Guid, Task<UserDto?>>? GetUserByIdHandler { get; set; }
        public Func<Guid, bool, bool, Task<bool>>? UpdateNotificationSettingsHandler { get; set; }
        public Func<Guid, Task<List<WebhookDto>>>? GetWebhooksHandler { get; set; }
        public Func<Guid, string, string, Task<WebhookDto>>? AddWebhookHandler { get; set; }
        public Func<Guid, Guid, Task<bool>>? DeleteWebhookHandler { get; set; }

        public Task<UserDto?> RegisterUser(User user) => Task.FromResult<UserDto?>(null);
        public Task<AuthenticatedUser?> GetUserFromSession(Guid id) => Task.FromResult<AuthenticatedUser?>(null);
        public Task<User?> GetUserFromEmail(string email) => Task.FromResult<User?>(null);
        public Task<Guid> CreateSession(Guid userId, TimeSpan duration) => Task.FromResult(Guid.Empty);
        public Task InvalidateSession(Guid sessionId) => Task.CompletedTask;

        public Task<UserDto?> GetUserById(Guid id) => 
            GetUserByIdHandler != null ? GetUserByIdHandler(id) : Task.FromResult<UserDto?>(null);
        
        public Task<bool> UpdateNotificationSettings(Guid userId, bool emailEnabled, bool discordEnabled) =>
            UpdateNotificationSettingsHandler != null ? UpdateNotificationSettingsHandler(userId, emailEnabled, discordEnabled) : Task.FromResult(true);
        
        public Task<List<WebhookDto>> GetWebhooks(Guid userId) =>
            GetWebhooksHandler != null ? GetWebhooksHandler(userId) : Task.FromResult(new List<WebhookDto>());
        
        public Task<WebhookDto> AddWebhook(Guid userId, string name, string url) =>
            AddWebhookHandler != null ? AddWebhookHandler(userId, name, url) : Task.FromResult(new WebhookDto { Id = Guid.NewGuid(), Name = name, WebhookUrl = url });
        
        public Task<bool> DeleteWebhook(Guid userId, Guid webhookId) =>
            DeleteWebhookHandler != null ? DeleteWebhookHandler(userId, webhookId) : Task.FromResult(true);

        public Task<bool> DeleteUser(Guid id) => Task.FromResult(true);
        public Task SavePasswordResetToken(Guid userId, string tokenHash, DateTime expiresAt) => Task.CompletedTask;
        public Task<User?> GetUserByPasswordResetToken(string tokenHash) => Task.FromResult<User?>(null);
        public Task<User?> GetUserCredentialsById(Guid id) => Task.FromResult<User?>(null);
        public Task<bool> UpdatePassword(Guid userId, string newPasswordHash, bool invalidateSession) => Task.FromResult(true);
    }

    public class FakeUserService : IUserService
    {
        public Func<string, Task>? RequestPasswordResetHandler { get; set; }
        public Func<ResetPasswordRequest, Task<(bool Success, string? ErrorMessage)>>? ResetPasswordHandler { get; set; }
        public Func<Guid, ChangePasswordRequest, Task<(bool Success, string? ErrorMessage)>>? ChangePasswordHandler { get; set; }

        public Task<UserDto?> RegisterUser(RegisterRequest request) => Task.FromResult<UserDto?>(null);
        public Task<LoginResult> LoginUser(LoginRequest request) => Task.FromResult(new LoginResult());
        public Task<AuthenticatedUser?> GetUserFromSession(Guid sessionId) => Task.FromResult<AuthenticatedUser?>(null);
        public Task Logout(Guid sessionId) => Task.CompletedTask;

        public Task RequestPasswordReset(string email) =>
            RequestPasswordResetHandler != null ? RequestPasswordResetHandler(email) : Task.CompletedTask;

        public Task<(bool Success, string? ErrorMessage)> ResetPassword(ResetPasswordRequest request) =>
            ResetPasswordHandler != null ? ResetPasswordHandler(request) : Task.FromResult<(bool, string?)>((true, null));

        public Task<(bool Success, string? ErrorMessage)> ChangePassword(Guid userId, ChangePasswordRequest request) =>
            ChangePasswordHandler != null ? ChangePasswordHandler(userId, request) : Task.FromResult<(bool, string?)>((true, null));
    }

    public class UserControllerTests
    {
        private readonly FakeUserRepository _repo;
        private readonly FakeUserService _service;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _repo = new FakeUserRepository();
            _service = new FakeUserService();
            _controller = new UserController(_service, NullLogger<UserController>.Instance);
        }

        private void SetAuthenticatedUser(Guid userId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Items["User"] = new AuthenticatedUser
            {
                Id = userId,
                SessionExp = DateTime.UtcNow.AddHours(1)
            };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task GetSettings_WhenAuthenticated_ReturnsOkWithSettings()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _repo.GetUserByIdHandler = (id) => Task.FromResult<UserDto?>(new UserDto 
            { 
                ID = id, 
                Username = "Test", 
                Email = "test@test.com",
                EmailNotificationsEnabled = true,
                DiscordNotificationsEnabled = false
            });

            var result = await _controller.GetSettings(_repo);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var userDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.True(userDto.EmailNotificationsEnabled);
            Assert.False(userDto.DiscordNotificationsEnabled);
        }

        [Fact]
        public async Task UpdateSettings_WhenValidRequest_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            bool updatedEmail = false;
            bool updatedDiscord = false;
            
            _repo.UpdateNotificationSettingsHandler = (id, email, discord) => 
            {
                updatedEmail = email;
                updatedDiscord = discord;
                return Task.FromResult(true);
            };

            var request = new UpdateSettingsRequest { EmailNotificationsEnabled = false, DiscordNotificationsEnabled = true };
            var result = await _controller.UpdateSettings(request, _repo);

            Assert.IsType<OkObjectResult>(result);
            Assert.False(updatedEmail);
            Assert.True(updatedDiscord);
        }

        [Fact]
        public async Task GetWebhooks_ReturnsWebhooksList()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _repo.GetWebhooksHandler = (id) => Task.FromResult(new List<WebhookDto> 
            { 
                new WebhookDto { Id = Guid.NewGuid(), Name = "Test", WebhookUrl = "http://test.com" } 
            });

            var result = await _controller.GetWebhooks(_repo);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var webhooks = Assert.IsAssignableFrom<IEnumerable<WebhookDto>>(okResult.Value);
            Assert.Single(webhooks);
        }

        [Fact]
        public async Task AddWebhook_ReturnsOkWithNewWebhook()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _repo.AddWebhookHandler = (id, name, url) => Task.FromResult(new WebhookDto 
            { 
                Id = Guid.NewGuid(), 
                Name = name, 
                WebhookUrl = url 
            });

            var request = new AddWebhookRequest { Name = "NewWebhook", WebhookUrl = "https://discord.com/api/webhooks/123456/abcdef" };
            var result = await _controller.AddWebhook(request, _repo);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var webhook = Assert.IsType<WebhookDto>(okResult.Value);
            Assert.Equal("NewWebhook", webhook.Name);
            Assert.Equal("https://discord.com/api/webhooks/123456/abcdef", webhook.WebhookUrl);
        }

        [Fact]
        public async Task AddWebhook_WhenExceedsLimit_ReturnsBadRequest()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _repo.GetWebhooksHandler = id => Task.FromResult(new List<WebhookDto>
            {
                new() { Id = Guid.NewGuid(), Name = "w1", WebhookUrl = "https://discord.com/api/webhooks/1/a", CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "w2", WebhookUrl = "https://discord.com/api/webhooks/2/b", CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "w3", WebhookUrl = "https://discord.com/api/webhooks/3/c", CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "w4", WebhookUrl = "https://discord.com/api/webhooks/4/d", CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "w5", WebhookUrl = "https://discord.com/api/webhooks/5/e", CreatedAt = DateTime.UtcNow },
            });

            var request = new AddWebhookRequest { Name = "w6", WebhookUrl = "https://discord.com/api/webhooks/6/f" };
            var result = await _controller.AddWebhook(request, _repo);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Maximum limit", badRequest.Value?.ToString());
        }

        [Theory]
        [InlineData("http://discord.com/api/webhooks/123/token")]
        [InlineData("https://evil.com/api/webhooks/123/token")]
        [InlineData("https://discord.com/api/other/123/token")]
        [InlineData("not-a-url")]
        public async Task AddWebhook_WhenInvalidUrl_ReturnsBadRequest(string url)
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);

            var request = new AddWebhookRequest { Name = "BadUrl", WebhookUrl = url };
            var result = await _controller.AddWebhook(request, _repo);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Only Discord webhook URLs are supported", badRequest.Value?.ToString());
        }

        [Fact]
        public async Task DeleteWebhook_WhenNotFound_ReturnsNotFound()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _repo.DeleteWebhookHandler = (id, webhookId) => Task.FromResult(false);

            var result = await _controller.DeleteWebhook(Guid.NewGuid(), _repo);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteWebhook_WhenFound_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _repo.DeleteWebhookHandler = (id, webhookId) => Task.FromResult(true);

            var result = await _controller.DeleteWebhook(Guid.NewGuid(), _repo);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_ReturnsOk()
        {
            string? requestedEmail = null;
            _service.RequestPasswordResetHandler = email =>
            {
                requestedEmail = email;
                return Task.CompletedTask;
            };

            var result = await _controller.ForgotPassword(new ForgotPasswordRequest { Email = "operator@cheaterwatch.com" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("operator@cheaterwatch.com", requestedEmail);
        }

        [Fact]
        public async Task ResetPassword_WhenInvalidToken_ReturnsBadRequest()
        {
            _service.ResetPasswordHandler = _ => Task.FromResult<(bool, string?)>((false, "Invalid or expired password reset token."));

            var result = await _controller.ResetPassword(new ResetPasswordRequest { Token = "bad", NewPassword = "NewSecurePassword123!" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid or expired password reset token.", badRequest.Value);
        }

        [Fact]
        public async Task ResetPassword_WhenValidToken_ReturnsOk()
        {
            _service.ResetPasswordHandler = _ => Task.FromResult<(bool, string?)>((true, null));

            var result = await _controller.ResetPassword(new ResetPasswordRequest { Token = "valid-token", NewPassword = "NewSecurePassword123!" });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_WhenUnauthenticated_ReturnsUnauthorized()
        {
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "NewSecurePassword123!" });

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task ChangePassword_WhenWrongCurrentPassword_ReturnsBadRequest()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _service.ChangePasswordHandler = (_, _) => Task.FromResult<(bool, string?)>((false, "Current password is incorrect."));

            var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = "wrong", NewPassword = "NewSecurePassword123!" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Current password is incorrect.", badRequest.Value);
        }

        [Fact]
        public async Task ChangePassword_WhenValid_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _service.ChangePasswordHandler = (_, _) => Task.FromResult<(bool, string?)>((true, null));

            var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = "OldPassword123!", NewPassword = "NewSecurePassword123!" });

            Assert.IsType<OkObjectResult>(result);
        }
    }
}

