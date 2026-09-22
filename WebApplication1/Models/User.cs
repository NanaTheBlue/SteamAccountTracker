namespace WebApplication1.Models
{
    public class User
    {
        public Guid ID { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public string? SessionId { get; set; }
        public DateTime? SessionExp { get; set; }
        public bool EmailNotificationsEnabled { get; set; } = true;
        public bool DiscordNotificationsEnabled { get; set; } = true;
    }

    public class AuthenticatedUser
    {
        public Guid Id { get; set; }
        public DateTime SessionExp { get; set; }
    }
}
