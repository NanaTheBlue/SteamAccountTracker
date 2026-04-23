using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IUserService
    {
        Task<User?> GetUser(Guid id);
        Task<User?> RegisterUser(User user);
    }
}
