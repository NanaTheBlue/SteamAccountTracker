namespace WebApplication1.Dtos
{
    public class SteamVanityResponse
    {
        public VanityResponse response { get; set; }
    }

    public class VanityResponse
    {
        public string steamid { get; set; }
        public int success { get; set; }
    }
}
