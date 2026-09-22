using WebApplication1.Dtos;
using WebApplication1.Models;

namespace WebApplication1.Repository
{
    public interface IUserRepository
    {
        Task<UserDto?> RegisterUser(User user);
        Task<AuthenticatedUser?> GetUserFromSession(Guid id);
        Task<User?> GetUserFromEmail(string email);
        Task<Guid> CreateSession(Guid userId, TimeSpan duration);
        Task InvalidateSession(Guid sessionId);
        
        Task<UserDto?> GetUserById(Guid id);
        Task<bool> UpdateNotificationSettings(Guid userId, bool emailEnabled, bool discordEnabled);
        
        Task<List<WebhookDto>> GetWebhooks(Guid userId);
        Task<WebhookDto> AddWebhook(Guid userId, string name, string url);
        Task<bool> DeleteWebhook(Guid userId, Guid webhookId);
    }
}
