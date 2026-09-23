namespace WebApplication1.Dtos
{
    public class SteamVanityResponse
    {
        public VanityResponse? response { get; set; }
    }

    public class VanityResponse
    {
        public string? steamid { get; set; }
        public int success { get; set; }
    }

    public class SteamPlayerSummariesResponse
    {
        public SteamPlayerSummariesInner? response { get; set; }
    }

    public class SteamPlayerSummariesInner
    {
        public List<SteamPlayerSummary>? players { get; set; }
    }

    public class SteamPlayerSummary
    {
        public string? steamid { get; set; }
        public string? personaname { get; set; }
        public string? profileurl { get; set; }
        public string? avatar { get; set; }
        public string? avatarmedium { get; set; }
        public string? avatarfull { get; set; }
    }
}
