using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Repository;

namespace WebApplication1.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IEmailService? _emailService;
        private readonly IConfiguration? _config;
        private static readonly TimeSpan SessionDuration = TimeSpan.FromDays(30);
        private static readonly TimeSpan PasswordResetDuration = TimeSpan.FromHours(1);

        public UserService(
            IUserRepository userRepository,
            ILogger<UserService> logger,
            IEmailService? emailService = null,
            IConfiguration? config = null)
        {
            _userRepository = userRepository;
            _logger = logger;
            _emailService = emailService;
            _config = config;
        }

        public async Task<AuthenticatedUser?> GetUserFromSession(Guid id)
        {
            var user = await _userRepository.GetUserFromSession(id);

            if (user == null) { return null; }

            // Check if session is expired
            if (user.SessionExp < DateTime.UtcNow)
            {
                return null;
            }

            return user;
        }

        public async Task<LoginResult> LoginUser(LoginRequest loginRequest)
        {
            try
            {
                var user = await _userRepository.GetUserFromEmail(loginRequest.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: Unknown email {Email}", loginRequest.Email);
                    return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
                }

                var result = BC.Verify(loginRequest.Password, user.PasswordHash);

                if (!result)
                {
                    _logger.LogWarning("Login failed: Wrong password for {Email}", loginRequest.Email);
                    return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
                }

                // Create a server-side session
                var sessionId = await _userRepository.CreateSession(user.ID, SessionDuration);

                return new LoginResult
                {
                    Success = true,
                    SessionId = sessionId,
                    User = new UserDto
                    {
                        ID = user.ID,
                        Username = user.Username,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", loginRequest.Email);
                return new LoginResult { Success = false, ErrorMessage = "An unexpected error occurred. Please try again later." };
            }
        }

        public async Task Logout(Guid sessionId)
        {
            await _userRepository.InvalidateSession(sessionId);
        }

        public async Task<UserDto?> RegisterUser(RegisterRequest registerRequest)
        {
            if (string.IsNullOrWhiteSpace(registerRequest.Username) ||
                string.IsNullOrWhiteSpace(registerRequest.Email) ||
                string.IsNullOrWhiteSpace(registerRequest.Password))
            {
                throw new ArgumentException("Username, Email and Password are required.");
            }

            if (!IsValidEmail(registerRequest.Email))
            {
                throw new ArgumentException("Invalid email format.");
            }

            if (registerRequest.Password.Length < 10)
            {
                throw new ArgumentException("Password must be at least 10 characters in length.");
            }

            if (registerRequest.Password.Length > 64)
            {
                throw new ArgumentException("Password must be 64 characters or less.");
            }

            registerRequest.Username = registerRequest.Username.Trim();
            if (registerRequest.Username.Length > 50)
            {
                throw new ArgumentException("Username must be 50 characters or less.");
            }

            // Normalize email to prevent casing duplicates
            registerRequest.Email = registerRequest.Email.Trim().ToLowerInvariant();

            var salt = BC.GenerateSalt();
            var hashedPW = BC.HashPassword(registerRequest.Password, salt);

            var user = new User
            {
                Username = registerRequest.Username.Trim(),
                Email = registerRequest.Email,
                PasswordHash = hashedPW,
            };

            var createdUser = await _userRepository.RegisterUser(user);

            if (createdUser == null)
            {
                return null;
            }

            return new UserDto
            {
                ID = createdUser.ID,
                Username = createdUser.Username,
                Email = createdUser.Email
            };
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            // Basic regex — catches the most common formatting errors
            return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static string HashToken(string rawToken)
        {
            var bytes = Encoding.UTF8.GetBytes(rawToken);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public async Task RequestPasswordReset(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                return;
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetUserFromEmail(normalizedEmail);
            if (user == null)
            {
                // Return silently to prevent email enumeration
                _logger.LogInformation("Password reset requested for non-existent email {Email}", normalizedEmail);
                return;
            }

            var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
            var rawToken = Convert.ToHexString(rawTokenBytes).ToLowerInvariant();
            var tokenHash = HashToken(rawToken);
            var expiresAt = DateTime.UtcNow.Add(PasswordResetDuration);

            await _userRepository.SavePasswordResetToken(user.ID, tokenHash, expiresAt);

            var frontendUrlConfig = _config?["FRONTEND_URL"] ?? "https://localhost:3000";
            var baseUrl = frontendUrlConfig
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim().TrimEnd('/'))
                .FirstOrDefault() ?? "https://localhost:3000";

            var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

            if (_emailService != null)
            {
                await _emailService.SendPasswordResetEmail(user.Email, user.Username, resetLink);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> ResetPassword(ResetPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token))
            {
                return (false, "Reset token is required.");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 10)
            {
                return (false, "Password must be at least 10 characters in length.");
            }

            if (request.NewPassword.Length > 64)
            {
                return (false, "Password must be 64 characters or less.");
            }

            var tokenHash = HashToken(request.Token.Trim());
            var user = await _userRepository.GetUserByPasswordResetToken(tokenHash);
            if (user == null)
            {
                return (false, "Invalid or expired password reset token.");
            }

            var salt = BC.GenerateSalt();
            var hashedPw = BC.HashPassword(request.NewPassword, salt);

            var updated = await _userRepository.UpdatePassword(user.ID, hashedPw, invalidateSession: true);
            if (!updated)
            {
                return (false, "Failed to reset password.");
            }

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> ChangePassword(Guid userId, ChangePasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return (false, "Current password and new password are required.");
            }

            if (request.NewPassword.Length < 10)
            {
                return (false, "New password must be at least 10 characters in length.");
            }

            if (request.NewPassword.Length > 64)
            {
                return (false, "New password must be 64 characters or less.");
            }

            var user = await _userRepository.GetUserCredentialsById(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            if (!BC.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return (false, "Current password is incorrect.");
            }

            var salt = BC.GenerateSalt();
            var hashedPw = BC.HashPassword(request.NewPassword, salt);

            var updated = await _userRepository.UpdatePassword(user.ID, hashedPw, invalidateSession: false);
            if (!updated)
            {
                return (false, "Failed to update password.");
            }

            return (true, null);
        }
    }
}
