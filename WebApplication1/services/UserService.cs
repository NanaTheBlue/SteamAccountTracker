using System.Text.RegularExpressions;
using WebApplication1.Models;
using WebApplication1.Repository;

namespace WebApplication1.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(24);

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
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
                    return new LoginResult { Success = false, ErrorMessage = "Invalid email or password." };
                }

                var result = BC.Verify(loginRequest.Password, user.PasswordHash);

                if (!result)
                {
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
    }
}
