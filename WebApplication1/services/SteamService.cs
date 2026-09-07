

using WebApplication1.Models;
using WebApplication1.Dtos;

namespace WebApplication1.services
{
    public class SteamService : ISteamService
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;


        public SteamService(HttpClient client, IConfiguration config)
        {
            _client = client;
            _apiKey = config["STEAM_API_KEY"]!;
        }




        public Task<string?> ConvertSteamID64(string steamid)
        {
            var parts = steamid.Split(':');

            if (parts.Length < 3)
                return Task.FromResult<string?>(null);

            if (!long.TryParse(parts[1], out var X))
                return Task.FromResult<string?>(null);

            var cleanY = parts[2].Replace("}", "").Replace("]", "");
            if (!long.TryParse(cleanY, out var Y))
                return Task.FromResult<string?>(null);

            const long baseId = 76561197960265728;
            var steam64 = baseId + (Y * 2) + X;

            return Task.FromResult<string?>(steam64.ToString());
        }


        public async Task<string?> ResolveSteamID64(string input)
        {
            input = input.Trim();

            // Already SteamID64
            if (input.All(char.IsDigit) && input.Length == 17 && input.StartsWith("7656"))
            {
                return input;
            }

            // Legacy SteamID
            if (input.StartsWith("STEAM_"))
            {
                return await ConvertSteamID64(input);
            }

            // Vanity URL
            if (input.Contains("steamcommunity.com/id/"))
            {
                return await ConvertVanityToSteamID64(input);
            }

            // Profile URL
            if (input.Contains("steamcommunity.com/profiles/"))
            {
                var uri = new Uri(input);

                return uri.Segments
                    .Last()
                    .Trim('/');
            }

            return null;
        }



        public async Task<string?> ConvertVanityToSteamID64(String vanityUrl)
        {
            // Example:
            // https://steamcommunity.com/id/nanatheblue/

            var uri = new Uri(vanityUrl);

            // Get the last non-empty segment
            var vanityString = uri.Segments
                .Last(segment => !string.IsNullOrWhiteSpace(segment))
                .Trim('/');



            var endpoint =
        $"https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/" +
        $"?key={_apiKey}&vanityurl={Uri.EscapeDataString(vanityString)}";

            var response =
                await _client.GetFromJsonAsync<SteamVanityResponse>(endpoint);

            if (response?.response?.success == 1)
            {
                return response.response.steamid;
            }

            return null;
        }





    }







}
