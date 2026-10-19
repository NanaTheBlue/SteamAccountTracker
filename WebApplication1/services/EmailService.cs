using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebApplication1.Services
{
    public class EmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _fromEmail;
        private readonly ILogger<EmailService> _logger;

        public EmailService(HttpClient httpClient, IConfiguration config, ILogger<EmailService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["RESEND_API_KEY"];
            _fromEmail = string.IsNullOrWhiteSpace(config["FROM_EMAIL"])
                ? "noreply@alerts.cheaterwatch.com"
                : config["FROM_EMAIL"]!;
        }

        public async Task SendPasswordResetEmail(string toEmail, string username, string resetLink)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning(
                    "RESEND_API_KEY is not configured. Skipping password reset email for {Email}. Reset link: {ResetLink}",
                    toEmail, resetLink);
                return;
            }

            var safeUsername = WebUtility.HtmlEncode(username);
            var safeLink = WebUtility.HtmlEncode(resetLink);

            var payload = new
            {
                from = $"CheaterWatch <{_fromEmail}>",
                to = new[] { toEmail },
                subject = "Reset Your CheaterWatch Password",
                html = $@"
                    <p>Hi {safeUsername},</p>
                    <p>We received a request to reset your CheaterWatch password.</p>
                    <p><a href=""{safeLink}"">Click here to set a new password</a></p>
                    <p>This link will expire in 1 hour. If you didn't request a password reset, you can safely ignore this email.</p>
                    <p>— CheaterWatch</p>
                "
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Failed to send password reset email via Resend ({StatusCode}): {ErrorBody}",
                    (int)response.StatusCode, errorBody);
            }
        }
    }
}

