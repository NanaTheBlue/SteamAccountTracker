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
    public class FakeSteamRepository : ISteamRepository
    {
        public Func<string, string, Task<bool>>? TrackSteamAccountHandler { get; set; }
        public Func<string, string, Task<bool>>? DeleteTrackedAccountHandler { get; set; }
        public Func<string, Task<List<TrackedAccountDto>>>? GetTrackedAccountsByUserHandler { get; set; }
        public Func<int, int, Task<List<TrackedAccountDto>>>? GetAllTrackedAccountsHandler { get; set; }
        public Func<List<BanUpdateEntry>, Task<List<NotificationEntry>>>? UpdateBanStatusHandler { get; set; }

        public Task<bool> TrackSteamAccount(string userId, string steamId64) =>
            TrackSteamAccountHandler != null ? TrackSteamAccountHandler(userId, steamId64) : Task.FromResult(true);

        public Task<bool> DeleteTrackedAccount(string userId, string steamId64) =>
            DeleteTrackedAccountHandler != null ? DeleteTrackedAccountHandler(userId, steamId64) : Task.FromResult(true);

        public Task<List<TrackedAccountDto>> GetTrackedAccountsByUser(string userId) =>
            GetTrackedAccountsByUserHandler != null ? GetTrackedAccountsByUserHandler(userId) : Task.FromResult(new List<TrackedAccountDto>());

        public Task<List<TrackedAccountDto>> GetAllTrackedAccounts(int offset, int limit) =>
            GetAllTrackedAccountsHandler != null ? GetAllTrackedAccountsHandler(offset, limit) : Task.FromResult(new List<TrackedAccountDto>());

        public Task<List<NotificationEntry>> UpdateBanStatusAndGetNotifications(List<BanUpdateEntry> updates) =>
            UpdateBanStatusHandler != null ? UpdateBanStatusHandler(updates) : Task.FromResult(new List<NotificationEntry>());
    }

    public class FakeSteamService : ISteamService
    {
        public Func<string, Task<string?>>? ResolveSteamID64Handler { get; set; }
        public Func<string, Task<string?>>? ConvertSteamID64Handler { get; set; }
        public Func<string, Task<string?>>? ConvertVanityToSteamID64Handler { get; set; }

        public Task<string?> ResolveSteamID64(string input) =>
            ResolveSteamID64Handler != null ? ResolveSteamID64Handler(input) : Task.FromResult<string?>(input);

        public Task<string?> ConvertSteamID64(string steamid) =>
            ConvertSteamID64Handler != null ? ConvertSteamID64Handler(steamid) : Task.FromResult<string?>(null);

        public Task<string?> ConvertVanityToSteamID64(string vanityUrl) =>
            ConvertVanityToSteamID64Handler != null ? ConvertVanityToSteamID64Handler(vanityUrl) : Task.FromResult<string?>(null);
    }

    public class SteamControllerTests
    {
        private readonly FakeSteamRepository _repo;
        private readonly FakeSteamService _service;
        private readonly SteamController _controller;

        public SteamControllerTests()
        {
            _repo = new FakeSteamRepository();
            _service = new FakeSteamService();
            _controller = new SteamController(_service, _repo, NullLogger<SteamController>.Instance);
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
        public async Task UntrackAccount_WhenUnauthenticated_ReturnsUnauthorized()
        {
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var result = await _controller.UntrackAccount("76561198070843489");

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UntrackAccount_WithEmptySteamId_ReturnsBadRequest(string steamId)
        {
            SetAuthenticatedUser(Guid.NewGuid());

            var result = await _controller.UntrackAccount(steamId);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UntrackAccount_WhenAccountNotFound_ReturnsNotFound()
        {
            SetAuthenticatedUser(Guid.NewGuid());
            _repo.DeleteTrackedAccountHandler = (_, _) => Task.FromResult(false);

            var result = await _controller.UntrackAccount("76561198070843489");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UntrackAccount_WhenAccountRemoved_ReturnsOk()
        {
            SetAuthenticatedUser(Guid.NewGuid());
            _repo.DeleteTrackedAccountHandler = (_, _) => Task.FromResult(true);

            var result = await _controller.UntrackAccount("76561198070843489");

            Assert.IsType<OkObjectResult>(result);
        }
    }
}

