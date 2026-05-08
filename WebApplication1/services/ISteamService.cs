namespace WebApplication1.services
{
    public interface ISteamService
    {


        Task<string> ConvertSteamID64(String steamid);

        Task<string?> ConvertVanityToSteamID64(String vanityUrl);
    }
}
