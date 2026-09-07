using WebApplication1.Dtos;

namespace WebApplication1.Repository
{
    public interface ISteamRepository
    {
        Task TrackSteamAccount(string userId, string steamId64);
        Task<List<TrackedAccountDto>> GetAllTrackedAccounts(int offset, int limit);
        Task<List<NotificationEntry>> UpdateBanStatusAndGetNotifications(List<BanUpdateEntry> updates);
    }
}
