using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Repository;
using WebApplication1.Services;
using Xunit;

namespace WebApplication1.Tests.Services
{
    public class FakeUserRepository : IUserRepository
    {
        public Func<User, Task<UserDto?>>? RegisterUserHandler { get; set; }
        public Func<Guid, Task<AuthenticatedUser?>>? GetUserFromSessionHandler { get; set; }
        public Func<string, Task<User?>>? GetUserFromEmailHandler { get; set; }
        public Func<Guid, TimeSpan, Task<Guid>>? CreateSessionHandler { get; set; }
        public Func<Guid, Task>? InvalidateSessionHandler { get; set; }

        public Task<UserDto?> RegisterUser(User user) =>
            RegisterUserHandler != null ? RegisterUserHandler(user) : Task.FromResult<UserDto?>(null);

        public Task<AuthenticatedUser?> GetUserFromSession(Guid id) =>
            GetUserFromSessionHandler != null ? GetUserFromSessionHandler(id) : Task.FromResult<AuthenticatedUser?>(null);

        public Task<User?> GetUserFromEmail(string email) =>
            GetUserFromEmailHandler != null ? GetUserFromEmailHandler(email) : Task.FromResult<User?>(null);

        public Task<Guid> CreateSession(Guid userId, TimeSpan duration) =>
            CreateSessionHandler != null ? CreateSessionHandler(userId, duration) : Task.FromResult(Guid.NewGuid());

        public Task InvalidateSession(Guid sessionId) =>
            InvalidateSessionHandler != null ? InvalidateSessionHandler(sessionId) : Task.CompletedTask;

        public Func<Guid, string, DateTime, Task>? SavePasswordResetTokenHandler { get; set; }
        public Func<string, Task<User?>>? GetUserByPasswordResetTokenHandler { get; set; }
        public Func<Guid, Task<User?>>? GetUserCredentialsByIdHandler { get; set; }
        public Func<Guid, string, bool, Task<bool>>? UpdatePasswordHandler { get; set; }

        public Task<UserDto?> GetUserById(Guid id) => Task.FromResult<UserDto?>(null);
        public Task<bool> UpdateNotificationSettings(Guid userId, bool emailEnabled, bool discordEnabled) => Task.FromResult(true);
        public Task<List<WebhookDto>> GetWebhooks(Guid userId) => Task.FromResult(new List<WebhookDto>());
        public Task<WebhookDto> AddWebhook(Guid userId, string name, string url) => Task.FromResult(new WebhookDto { Id = Guid.NewGuid(), Name = name, WebhookUrl = url });
        public Task<bool> DeleteWebhook(Guid userId, Guid webhookId) => Task.FromResult(true);
        public Task<bool> DeleteUser(Guid id) => Task.FromResult(true);

        public Task SavePasswordResetToken(Guid userId, string tokenHash, DateTime expiresAt) =>
            SavePasswordResetTokenHandler != null ? SavePasswordResetTokenHandler(userId, tokenHash, expiresAt) : Task.CompletedTask;

        public Task<User?> GetUserByPasswordResetToken(string tokenHash) =>
            GetUserByPasswordResetTokenHandler != null ? GetUserByPasswordResetTokenHandler(tokenHash) : Task.FromResult<User?>(null);

        public Task<User?> GetUserCredentialsById(Guid id) =>
            GetUserCredentialsByIdHandler != null ? GetUserCredentialsByIdHandler(id) : Task.FromResult<User?>(null);

        public Task<bool> UpdatePassword(Guid userId, string newPasswordHash, bool invalidateSession) =>
            UpdatePasswordHandler != null ? UpdatePasswordHandler(userId, newPasswordHash, invalidateSession) : Task.FromResult(true);
    }

    public class FakeEmailService : IEmailService
    {
        public string? LastToEmail { get; private set; }
        public string? LastUsername { get; private set; }
        public string? LastResetLink { get; private set; }

        public Task SendPasswordResetEmail(string toEmail, string username, string resetLink)
        {
            LastToEmail = toEmail;
            LastUsername = username;
            LastResetLink = resetLink;
            return Task.CompletedTask;
        }
    }

    public class UserServiceTests
    {
        private readonly FakeUserRepository _userRepository;
        private readonly FakeEmailService _emailService;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _userRepository = new FakeUserRepository();
            _emailService = new FakeEmailService();
            _service = new UserService(_userRepository, NullLogger<UserService>.Instance, _emailService);
        }

        [Theory]
        [InlineData("", "test@example.com", "Password123!")]
        [InlineData("testuser", "", "Password123!")]
        [InlineData("testuser", "test@example.com", "")]
        [InlineData("   ", "test@example.com", "Password123!")]
        public async Task RegisterUser_WithMissingFields_ThrowsArgumentException(string username, string email, string password)
        {
            var request = new RegisterRequest { Username = username, Email = email, Password = password };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.RegisterUser(request));
            Assert.Contains("Username, Email and Password are required", ex.Message);
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("user@")]
        [InlineData("@example.com")]
        [InlineData("user@domain")]
        public async Task RegisterUser_WithInvalidEmail_ThrowsArgumentException(string email)
        {
            var request = new RegisterRequest { Username = "validuser", Email = email, Password = "Password123!" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.RegisterUser(request));
            Assert.Contains("Invalid email format", ex.Message);
        }

        [Fact]
        public async Task RegisterUser_WithShortPassword_ThrowsArgumentException()
        {
            var request = new RegisterRequest { Username = "validuser", Email = "test@example.com", Password = "short" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.RegisterUser(request));
            Assert.Contains("Password must be at least 10 characters", ex.Message);
        }

        [Fact]
        public async Task RegisterUser_WithValidRequest_ReturnsUserDto()
        {
            User? capturedUser = null;
            var expectedId = Guid.NewGuid();

            _userRepository.RegisterUserHandler = (user) =>
            {
                capturedUser = user;
                return Task.FromResult<UserDto?>(new UserDto
                {
                    ID = expectedId,
                    Username = user.Username,
                    Email = user.Email
                });
            };

            var request = new RegisterRequest
            {
                Username = "  NewUser  ",
                Email = "  User@Example.COM  ",
                Password = "SuperSecurePassword123!"
            };

            var result = await _service.RegisterUser(request);

            Assert.NotNull(result);
            Assert.Equal(expectedId, result.ID);
            Assert.Equal("NewUser", result.Username);
            Assert.Equal("user@example.com", result.Email);

            Assert.NotNull(capturedUser);
            Assert.Equal("NewUser", capturedUser.Username);
            Assert.Equal("user@example.com", capturedUser.Email);
            Assert.True(BCrypt.Net.BCrypt.Verify("SuperSecurePassword123!", capturedUser.PasswordHash));
        }

        [Fact]
        public async Task RegisterUser_WhenRepositoryReturnsNull_ReturnsNull()
        {
            _userRepository.RegisterUserHandler = _ => Task.FromResult<UserDto?>(null);

            var request = new RegisterRequest
            {
                Username = "existinguser",
                Email = "taken@example.com",
                Password = "ValidPassword123!"
            };

            var result = await _service.RegisterUser(request);

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginUser_WhenUserNotFound_ReturnsFailedResult()
        {
            _userRepository.GetUserFromEmailHandler = _ => Task.FromResult<User?>(null);

            var result = await _service.LoginUser(new LoginRequest
            {
                Email = "notfound@example.com",
                Password = "Password123!"
            });

            Assert.False(result.Success);
            Assert.Equal("Invalid email or password.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_WhenPasswordIncorrect_ReturnsFailedResult()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
            _userRepository.GetUserFromEmailHandler = _ => Task.FromResult<User?>(new User
            {
                ID = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = hash
            });

            var result = await _service.LoginUser(new LoginRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword999!"
            });

            Assert.False(result.Success);
            Assert.Equal("Invalid email or password.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_WhenValidCredentials_ReturnsSuccessWithSession()
        {
            var userId = Guid.NewGuid();
            var expectedSessionId = Guid.NewGuid();
            var password = "CorrectPassword123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            _userRepository.GetUserFromEmailHandler = _ => Task.FromResult<User?>(new User
            {
                ID = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = hash
            });

            _userRepository.CreateSessionHandler = (id, duration) =>
            {
                Assert.Equal(userId, id);
                Assert.Equal(TimeSpan.FromDays(30), duration);
                return Task.FromResult(expectedSessionId);
            };

            var result = await _service.LoginUser(new LoginRequest
            {
                Email = "test@example.com",
                Password = password
            });

            Assert.True(result.Success);
            Assert.Equal(expectedSessionId, result.SessionId);
            Assert.NotNull(result.User);
            Assert.Equal(userId, result.User.ID);
            Assert.Equal("testuser", result.User.Username);
        }

        [Fact]
        public async Task GetUserFromSession_WhenNotFound_ReturnsNull()
        {
            _userRepository.GetUserFromSessionHandler = _ => Task.FromResult<AuthenticatedUser?>(null);

            var result = await _service.GetUserFromSession(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserFromSession_WhenSessionExpired_ReturnsNull()
        {
            _userRepository.GetUserFromSessionHandler = id => Task.FromResult<AuthenticatedUser?>(new AuthenticatedUser
            {
                Id = id,
                SessionExp = DateTime.UtcNow.AddMinutes(-5)
            });

            var result = await _service.GetUserFromSession(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserFromSession_WhenSessionActive_ReturnsUser()
        {
            var sessionId = Guid.NewGuid();
            var expectedUser = new AuthenticatedUser
            {
                Id = Guid.NewGuid(),
                SessionExp = DateTime.UtcNow.AddHours(2)
            };

            _userRepository.GetUserFromSessionHandler = id => Task.FromResult<AuthenticatedUser?>(expectedUser);

            var result = await _service.GetUserFromSession(sessionId);

            Assert.NotNull(result);
            Assert.Equal(expectedUser.Id, result.Id);
        }

        [Fact]
        public async Task Logout_CallsInvalidateSession()
        {
            var sessionId = Guid.NewGuid();
            Guid? invalidatedId = null;

            _userRepository.InvalidateSessionHandler = id =>
            {
                invalidatedId = id;
                return Task.CompletedTask;
            };

            await _service.Logout(sessionId);

            Assert.Equal(sessionId, invalidatedId);
        }

        [Fact]
        public async Task RegisterUser_WithUsernameExceeding50Chars_ThrowsArgumentException()
        {
            var request = new RegisterRequest
            {
                Username = new string('a', 51),
                Email = "valid@example.com",
                Password = "Password123!"
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.RegisterUser(request));
            Assert.Contains("50 characters or less", ex.Message);
        }

        [Fact]
        public async Task RequestPasswordReset_WhenEmailDoesNotExist_DoesNotSendEmail()
        {
            _userRepository.GetUserFromEmailHandler = _ => Task.FromResult<User?>(null);

            await _service.RequestPasswordReset("unknown@example.com");

            Assert.Null(_emailService.LastToEmail);
        }

        [Fact]
        public async Task RequestPasswordReset_WhenUserExists_SavesTokenHashAndSendsEmail()
        {
            var userId = Guid.NewGuid();
            string? savedTokenHash = null;

            _userRepository.GetUserFromEmailHandler = _ => Task.FromResult<User?>(new User
            {
                ID = userId,
                Username = "TestOperator",
                Email = "operator@example.com",
                PasswordHash = "hash"
            });

            _userRepository.SavePasswordResetTokenHandler = (id, tokenHash, exp) =>
            {
                savedTokenHash = tokenHash;
                return Task.CompletedTask;
            };

            await _service.RequestPasswordReset("Operator@Example.com");

            Assert.NotNull(savedTokenHash);
            Assert.Equal("operator@example.com", _emailService.LastToEmail);
            Assert.Equal("TestOperator", _emailService.LastUsername);
            Assert.Contains("/reset-password?token=", _emailService.LastResetLink);
        }

        [Fact]
        public async Task ResetPassword_WithValidToken_HashesTokenAndUpdatesPasswordInvalidatingSession()
        {
            var userId = Guid.NewGuid();
            var rawToken = "my-secret-reset-token";
            var expectedHash = UserService.HashToken(rawToken);
            bool? invalidatedSession = null;
            string? storedPasswordHash = null;

            _userRepository.GetUserByPasswordResetTokenHandler = tokenHash =>
            {
                Assert.Equal(expectedHash, tokenHash);
                return Task.FromResult<User?>(new User
                {
                    ID = userId,
                    Username = "TestOperator",
                    Email = "operator@example.com",
                    PasswordHash = "oldhash"
                });
            };

            _userRepository.UpdatePasswordHandler = (id, newHash, invalidate) =>
            {
                storedPasswordHash = newHash;
                invalidatedSession = invalidate;
                return Task.FromResult(true);
            };

            var (success, error) = await _service.ResetPassword(new ResetPasswordRequest
            {
                Token = rawToken,
                NewPassword = "BrandNewPassword123!"
            });

            Assert.True(success);
            Assert.Null(error);
            Assert.True(invalidatedSession);
            Assert.NotNull(storedPasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("BrandNewPassword123!", storedPasswordHash));
        }

        [Fact]
        public async Task ChangePassword_WhenCurrentPasswordMatches_UpdatesPasswordWithoutInvalidatingSession()
        {
            var userId = Guid.NewGuid();
            var oldHash = BCrypt.Net.BCrypt.HashPassword("CurrentPass123!");
            bool? invalidatedSession = null;
            string? newStoredHash = null;

            _userRepository.GetUserCredentialsByIdHandler = _ => Task.FromResult<User?>(new User
            {
                ID = userId,
                Username = "TestOperator",
                Email = "operator@example.com",
                PasswordHash = oldHash
            });

            _userRepository.UpdatePasswordHandler = (id, hash, invalidate) =>
            {
                newStoredHash = hash;
                invalidatedSession = invalidate;
                return Task.FromResult(true);
            };

            var (success, error) = await _service.ChangePassword(userId, new ChangePasswordRequest
            {
                CurrentPassword = "CurrentPass123!",
                NewPassword = "UpdatedPass456!"
            });

            Assert.True(success);
            Assert.Null(error);
            Assert.False(invalidatedSession);
            Assert.NotNull(newStoredHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("UpdatedPass456!", newStoredHash));
        }
    }
}

