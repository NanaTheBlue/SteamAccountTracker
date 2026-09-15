using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Dtos
{
    public class TrackSteamRequest
    {
        /// <summary>
        /// Any Steam identifier: SteamID64, legacy STEAM_ID (STEAM_X:Y:Z),
        /// vanity URL (https://steamcommunity.com/id/name), or profile URL
        /// (https://steamcommunity.com/profiles/12345).
        /// </summary>
        [Required]
        [MaxLength(300)]
        public required string SteamInput { get; set; }
    }
}

