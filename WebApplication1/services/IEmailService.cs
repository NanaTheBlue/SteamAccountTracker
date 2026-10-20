namespace WebApplication1.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmail(string toEmail, string username, string resetLink);
    }
}

