using WebApplication1.Dtos;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IUserService
    {
        Task<UserDto?> RegisterUser(RegisterRequest registerRequest);
        Task<AuthenticatedUser?> GetUserFromSession(Guid id);
        Task<LoginResult> LoginUser(LoginRequest loginRequest);
        Task Logout(Guid sessionId);
        Task RequestPasswordReset(string email);
        Task<(bool Success, string? ErrorMessage)> ResetPassword(ResetPasswordRequest request);
        Task<(bool Success, string? ErrorMessage)> ChangePassword(Guid userId, ChangePasswordRequest request);
    }
}
