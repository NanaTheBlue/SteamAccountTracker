using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Controllers;
using WebApplication1.Dtos;
using Xunit;

namespace WebApplication1.Tests.Controllers
{
    public class InternalSteamControllerTests
    {
        private readonly FakeSteamRepository _repo;
        private readonly InternalSteamController _controller;

        public InternalSteamControllerTests()
        {
            _repo = new FakeSteamRepository();
            _controller = new InternalSteamController(_repo, NullLogger<InternalSteamController>.Instance);
        }

        [Fact]
        public async Task GetAccountsToScan_WithNegativeOffset_ReturnsBadRequest()
        {
            var result = await _controller.GetAccountsToScan(offset: -1, limit: 100);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetAccountsToScan_WithZeroOrNegativeLimit_ReturnsBadRequest()
        {
            var result = await _controller.GetAccountsToScan(offset: 0, limit: 0);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostBanUpdates_WithEmptyList_ReturnsBadRequest()
        {
            var request = new BanUpdateRequest { Updates = new List<BanUpdateEntry>() };

            var result = await _controller.PostBanUpdates(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostBanUpdates_WithInvalidSteamIds_ReturnsBadRequest()
        {
            var request = new BanUpdateRequest
            {
                Updates = new List<BanUpdateEntry>
                {
                    new BanUpdateEntry { SteamId64 = "short_id" }
                }
            };

            var result = await _controller.PostBanUpdates(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostBanUpdates_WithValidEntries_ReturnsOk()
        {
            var request = new BanUpdateRequest
            {
                Updates = new List<BanUpdateEntry>
                {
                    new BanUpdateEntry
                    {
                        SteamId64 = "76561198070843489",
                        VACBanned = true,
                        NumberOfVACBans = 1
                    }
                }
            };

            var result = await _controller.PostBanUpdates(request);

            Assert.IsType<OkObjectResult>(result);
        }
    }
}

