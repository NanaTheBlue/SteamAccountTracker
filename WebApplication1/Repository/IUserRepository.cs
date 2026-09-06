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
    }
}
