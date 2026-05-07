

using WebApplication1.Models;

namespace WebApplication1.services
{
    public class SteamService : ISteamService
    {
        private readonly HttpClient _client;

        public SteamService(HttpClient client)
        {
            _client = client;
            _apiKey = config["Steam:ApiKey"];
        }




        public  Task<string> ConvertSteamID64(String steamid)
        {
            var parts = steamid.Split(':');

            var X = long.Parse(parts[1]);
            var Y = long.Parse(parts[2].Replace("}", "").Replace("]", ""));

            const long baseId = 76561197960265728;

            var steam64 = baseId + (Y * 2) + X;

            return Task.FromResult(steam64.ToString());
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
        $"?key={_apiKey}&vanityurl={vanityString}";

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
