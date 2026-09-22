using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Dtos
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public required string Email { get; set; }

        [Required]
        [MinLength(10)]
        public required string Password { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        public string? Username { get; set; }

        [Required]
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
        public bool EmailNotificationsEnabled { get; set; }
        public bool DiscordNotificationsEnabled { get; set; }
    }

    public class UpdateSettingsRequest
    {
        [Required]
        public bool EmailNotificationsEnabled { get; set; }
        
        [Required]
        public bool DiscordNotificationsEnabled { get; set; }
    }

    public class WebhookDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string WebhookUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddWebhookRequest
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(1000)]
        [Url]
        public required string WebhookUrl { get; set; }
    }
}

