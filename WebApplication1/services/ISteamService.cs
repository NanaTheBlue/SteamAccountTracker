namespace WebApplication1.Services
{
    public interface ISteamService
    {


        Task<string?> ConvertSteamID64(String steamid);

        Task<string?> ConvertVanityToSteamID64(String vanityUrl);
        Task<string?> ResolveSteamID64(string input);
    }
}
