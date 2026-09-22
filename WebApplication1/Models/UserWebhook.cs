namespace WebApplication1.Models
{
    public class UserWebhook
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public required string Name { get; set; }
        public required string WebhookUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

