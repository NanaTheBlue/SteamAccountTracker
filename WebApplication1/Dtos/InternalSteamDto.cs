namespace WebApplication1.Dtos
{
    // --- Internal API: GET /api/internal/steam/accounts-to-scan ---

    public class TrackedAccountDto
    {
        public required string SteamId64 { get; set; }
        public bool VACBanned { get; set; }
        public int NumberOfVACBans { get; set; }
        public int NumberOfGameBans { get; set; }
        public bool CommunityBanned { get; set; }
    }

    // --- Internal API: POST /api/internal/steam/ban-updates ---

    public class BanUpdateRequest
    {
        public required List<BanUpdateEntry> Updates { get; set; }
    }

    public class BanUpdateEntry
    {
        public required string SteamId64 { get; set; }
        public bool VACBanned { get; set; }
        public int NumberOfVACBans { get; set; }
        public int NumberOfGameBans { get; set; }
        public bool CommunityBanned { get; set; }
    }

    public class BanUpdateResponse
    {
        public required List<NotificationEntry> Notifications { get; set; }
    }

    public class NotificationEntry
    {
        public required string Email { get; set; }
        public required string Username { get; set; }
        public required string SteamId64 { get; set; }
        public required string BanType { get; set; }
    }
}

