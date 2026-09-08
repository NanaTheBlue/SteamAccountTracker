using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WebApplication1.Dtos;
using WebApplication1.services;
using Xunit;

namespace WebApplication1.Tests.Services
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    public class SteamServiceTests
    {
        private readonly IConfiguration _config;
        private const string TestApiKey = "TEST_STEAM_API_KEY_12345";

        public SteamServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "STEAM_API_KEY", TestApiKey }
            };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private SteamService CreateService(Func<HttpRequestMessage, HttpResponseMessage>? handler = null)
        {
            var httpHandler = new FakeHttpMessageHandler(handler ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)));
            var client = new HttpClient(httpHandler);
            return new SteamService(client, _config);
        }

        [Theory]
        [InlineData("STEAM_0:0:12345", "76561197960290418")]
        [InlineData("STEAM_0:1:55288880", "76561198070843488")]
        [InlineData("STEAM_0:1:55288880", "76561198070843489")]
        [InlineData("STEAM_1:1:55288880", "76561198070843489")]
        [InlineData("STEAM_0:1:55288880}", "76561198070843488")]
        [InlineData("STEAM_0:1:55288880]", "76561198070843488")]
        [InlineData("STEAM_0:1:55288880}", "76561198070843489")]
        [InlineData("STEAM_0:1:55288880]", "76561198070843489")]
        public async Task ConvertSteamID64_WithValidLegacySteamId_ReturnsCorrectSteamID64(string legacyId, string expectedSteam64)
        {
            var service = CreateService();

            var result = await service.ConvertSteamID64(legacyId);

            Assert.Equal(expectedSteam64, result);
        }

        [Theory]
        [InlineData("STEAM_0:0")]
        [InlineData("INVALID")]
        [InlineData("STEAM_X:0:12345")]
        [InlineData("STEAM_0:X:12345")]
        [InlineData("STEAM_0:Y:12345")]
        [InlineData("STEAM_0:0:abc")]
        public async Task ConvertSteamID64_WithInvalidFormat_ReturnsNull(string invalidLegacyId)
        {
            var service = CreateService();

            var result = await service.ConvertSteamID64(invalidLegacyId);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("76561198070843488", "76561198070843488")]
        [InlineData("  76561198070843489  ", "76561198070843489")]
        public async Task ResolveSteamID64_WhenInputIsAlreadySteamID64_ReturnsInput(string input, string expected)
        {
            var service = CreateService();

            var result = await service.ResolveSteamID64(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ResolveSteamID64_WhenInputIsLegacySteamId_ConvertsCorrectly()
        {
            var service = CreateService();

            var result = await service.ResolveSteamID64("STEAM_0:1:55288880");

            Assert.Equal("76561198070843488", result);
            Assert.Equal("76561198070843489", result);
        }

        [Theory]
        [InlineData("https://steamcommunity.com/profiles/76561198070843489", "76561198070843489")]
        [InlineData("https://steamcommunity.com/profiles/76561198070843489/", "76561198070843489")]
        [InlineData("http://steamcommunity.com/profiles/76561198070843489", "76561198070843489")]
        public async Task ResolveSteamID64_WhenInputIsProfileUrl_ExtractsSteamID64(string profileUrl, string expectedId)
        {
            var service = CreateService();

            var result = await service.ResolveSteamID64(profileUrl);

            Assert.Equal(expectedId, result);
        }

        [Fact]
        public async Task ResolveSteamID64_WhenInputIsVanityUrl_ResolvesViaApi()
        {
            HttpRequestMessage? capturedRequest = null;
            var service = CreateService(request =>
            {
                capturedRequest = request;
                var json = JsonSerializer.Serialize(new SteamVanityResponse
                {
                    response = new VanityResponse
                    {
                        success = 1,
                        steamid = "76561198070843489"
                    }
                });

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

            var result = await service.ResolveSteamID64("https://steamcommunity.com/id/nanatheblue/");

            Assert.Equal("76561198070843489", result);
            Assert.NotNull(capturedRequest);
            Assert.Contains($"key={TestApiKey}", capturedRequest.RequestUri?.Query);
            Assert.Contains("vanityurl=nanatheblue", capturedRequest.RequestUri?.Query);
        }

        [Fact]
        public async Task ResolveSteamID64_WhenVanityUrlNotFound_ReturnsNull()
        {
            var service = CreateService(_ =>
            {
                var json = JsonSerializer.Serialize(new SteamVanityResponse
                {
                    response = new VanityResponse
                    {
                        success = 42, // No match
                        steamid = null
                    }
                });

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

            var result = await service.ResolveSteamID64("https://steamcommunity.com/id/nonexistentuser123");

            Assert.Null(result);
        }

        [Theory]
        [InlineData("not-a-valid-input")]
        [InlineData("https://example.com/someuser")]
        [InlineData("12345")]
        public async Task ResolveSteamID64_WhenInputUnrecognized_ReturnsNull(string input)
        {
            var service = CreateService();

            var result = await service.ResolveSteamID64(input);

            Assert.Null(result);
        }
    }
}

