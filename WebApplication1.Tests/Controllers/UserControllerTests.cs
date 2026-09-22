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
    }

    public class FakeUserService : IUserService
    {
        public Task<UserDto?> RegisterUser(RegisterRequest request) => Task.FromResult<UserDto?>(null);
        public Task<LoginResult> LoginUser(LoginRequest request) => Task.FromResult(new LoginResult());
        public Task<AuthenticatedUser?> GetUserFromSession(Guid sessionId) => Task.FromResult<AuthenticatedUser?>(null);
        public Task Logout(Guid sessionId) => Task.CompletedTask;
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

            var request = new AddWebhookRequest { Name = "NewWebhook", WebhookUrl = "http://discord.com" };
            var result = await _controller.AddWebhook(request, _repo);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var webhook = Assert.IsType<WebhookDto>(okResult.Value);
            Assert.Equal("NewWebhook", webhook.Name);
            Assert.Equal("http://discord.com", webhook.WebhookUrl);
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
    }
}

