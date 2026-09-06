using Microsoft.Data.SqlClient;
using WebApplication1.Models;

namespace WebApplication1.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IConfiguration config, ILogger<UserRepository> logger)
        {
            _connectionString = config.GetConnectionString("CONNECTION_STRING")
                ?? throw new InvalidOperationException("Connection string 'CONNECTION_STRING' not found.");
            _logger = logger;
        }

        public async Task<User?> GetUserFromEmail(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            try
            {
                using var cmd = new SqlCommand("SELECT Id, Username, Email, Passwordhash FROM Users WHERE Email = @Email;", conn);
                cmd.Parameters.AddWithValue("@Email", email);
                using var reader = await cmd.ExecuteReaderAsync();
                var idOrdinal = reader.GetOrdinal("Id");
                var usernameOrdinal = reader.GetOrdinal("Username");
                var emailOrdinal = reader.GetOrdinal("Email");
                var passwordHashOrdinal = reader.GetOrdinal("Passwordhash");
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        ID = reader.GetGuid(idOrdinal),
                        Username = reader.GetString(usernameOrdinal),
                        Email = reader.GetString(emailOrdinal),
                        PasswordHash = reader.GetString(passwordHashOrdinal)
                    };
                }
                return null;
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Failed to get user by email");
                throw;
            }
        }

        public async Task<UserDto?> RegisterUser(User user)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand("INSERT INTO Users (Username, Email, Passwordhash) OUTPUT inserted.Id, inserted.Username, inserted.Email VALUES (@username, @email, @passwordhash);", conn);
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@passwordhash", user.PasswordHash);

                using var reader = await cmd.ExecuteReaderAsync();
                var idOrdinal = reader.GetOrdinal("Id");
                var usernameOrdinal = reader.GetOrdinal("Username");
                var emailOrdinal = reader.GetOrdinal("Email");
                if (await reader.ReadAsync())
                {
                    return new UserDto
                    {
                        ID = reader.GetGuid(idOrdinal),
                        Username = reader.GetString(usernameOrdinal),
                        Email = reader.GetString(emailOrdinal),
                    };
                }

                return null;
            }
            catch (SqlException e) when (e.Number == 2627)
            {
                return null;
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Failed to register user");
                throw;
            }
        }

        public async Task<AuthenticatedUser?> GetUserFromSession(Guid id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand("SELECT Id, Session_exp FROM Users WHERE Session_Id = @session_id;", conn);
                cmd.Parameters.AddWithValue("@session_id", id);
                using var reader = await cmd.ExecuteReaderAsync();

                var idOrdinal = reader.GetOrdinal("Id");
                var sessionExpOrdinal = reader.GetOrdinal("Session_exp");

                if (await reader.ReadAsync())
                {
                    return new AuthenticatedUser
                    {
                        Id = reader.GetGuid(idOrdinal),
                        SessionExp = reader.GetDateTime(sessionExpOrdinal)
                    };
                }

                return null;
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Failed to get user from session");
                throw;
            }
        }

        public async Task<Guid> CreateSession(Guid userId, TimeSpan duration)
        {
            var sessionId = Guid.NewGuid();
            var expiry = DateTime.UtcNow.Add(duration);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand("UPDATE Users SET Session_Id = @sessionId, Session_exp = @expiry WHERE Id = @userId;", conn);
                cmd.Parameters.AddWithValue("@sessionId", sessionId);
                cmd.Parameters.AddWithValue("@expiry", expiry);
                cmd.Parameters.AddWithValue("@userId", userId);

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    throw new InvalidOperationException("User not found when creating session.");
                }

                return sessionId;
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Failed to create session for user {UserId}", userId);
                throw;
            }
        }

        public async Task InvalidateSession(Guid sessionId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand("UPDATE Users SET Session_Id = NULL, Session_exp = NULL WHERE Session_Id = @sessionId;", conn);
                cmd.Parameters.AddWithValue("@sessionId", sessionId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Failed to invalidate session");
                throw;
            }
        }
    }
}
