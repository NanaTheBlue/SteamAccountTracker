namespace WebApplication1.Models
{
    public class User
    {
        public Guid ID { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public string ?SessionId { get; set; }

        public DateTime ? SessionExp { get; set; }
    }

    public class RegisterRequest
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }


    public class AuthenticatedUser
    {
        public Guid Id { get; set; }

        public DateTime SessionExp { get; set; }
    }

    public class LoginRequest
    {
        public required string Email { get; set; }

        public string ? Username { get; set; }

        public required string Password { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public UserDto? User { get; set; }
        public Guid? SessionId { get; set; }
    }

    public class UserDto
    {
        public Guid ID { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
    }
}
