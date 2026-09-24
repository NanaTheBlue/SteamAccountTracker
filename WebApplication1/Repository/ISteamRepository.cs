using WebApplication1.Dtos;

namespace WebApplication1.Repository
{
    public interface ISteamRepository
    {
        Task<bool> ProfileDataExists(string steamId64);
        Task<bool> TrackSteamAccount(string userId, string steamId64, string? personaName, string? avatarUrl, string? avatarFullUrl);
        Task<bool> DeleteTrackedAccount(string userId, string steamId64);
        Task<List<TrackedAccountDto>> GetTrackedAccountsByUser(string userId);
        Task<List<TrackedAccountDto>> GetAllTrackedAccounts(int offset, int limit);
        Task<List<NotificationEntry>> UpdateBanStatusAndGetNotifications(List<BanUpdateEntry> updates);
        Task UpdateLastScannedAt(List<string> steamId64s);
    }
}
