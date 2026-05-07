using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IUserService
    {
       
        Task<UserDto?> RegisterUser(RegisterRequest registerRequest);
        Task<AuthenticatedUser?> GetUserFromSession(Guid id);

        Task<LoginResult> LoginUser(LoginRequest loginRequest);


    }
}
