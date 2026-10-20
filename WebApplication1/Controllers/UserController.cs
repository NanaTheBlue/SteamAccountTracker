using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("api")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            if (registerRequest == null)
            {
                return BadRequest("User payload cannot be null.");
            }

            try
            {
                var user = await _userService.RegisterUser(registerRequest);
                if (user == null)
                {
                    return Conflict("An account with this email already exists.");
                }

                // Automatically log the user in right after registering
                var loginResult = await _userService.LoginUser(new LoginRequest 
                { 
                    Email = registerRequest.Email, 
                    Password = registerRequest.Password 
                });

                if (loginResult.Success && loginResult.SessionId.HasValue)
                {
                    Response.Cookies.Append("sessionId", loginResult.SessionId.Value.ToString(), new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        MaxAge = TimeSpan.FromDays(30),
                        Path = "/"
                    });
                }

                return Ok(new { message = "User registered successfully.", user });
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during user registration");
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null)
            {
                return BadRequest("Request payload cannot be null.");
            }

            var result = await _userService.LoginUser(loginRequest);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            // Set the session cookie with security flags
            Response.Cookies.Append("sessionId", result.SessionId!.Value.ToString(), new CookieOptions
            {
                HttpOnly = true,   // Prevents JavaScript access (XSS protection)
                Secure = true,     // Only sent over HTTPS
                SameSite = SameSiteMode.Strict, // CSRF protection
                MaxAge = TimeSpan.FromDays(30),
                Path = "/"
            });

            return Ok(new { message = "Login successful.", user = result.User });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var sessionCookie = Request.Cookies["sessionId"];
            if (!string.IsNullOrEmpty(sessionCookie) && Guid.TryParse(sessionCookie, out var sessionId))
            {
                await _userService.Logout(sessionId);
            }

            // Clear the cookie regardless
            Response.Cookies.Delete("sessionId", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            try
            {
                await _userService.RequestPasswordReset(request.Email);
                return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing password reset request");
                return StatusCode(500, "An error occurred while processing the password reset request.");
            }
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request payload cannot be null.");
            }

            try
            {
                var result = await _userService.ResetPassword(request);
                if (!result.Success)
                {
                    return BadRequest(result.ErrorMessage);
                }

                return Ok(new { message = "Password has been reset successfully. You can now sign in." });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error resetting password");
                return StatusCode(500, "An error occurred while resetting the password.");
            }
        }

        [HttpPost("change-password")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = (AuthenticatedUser?)HttpContext.Items["User"];
            if (user == null) return Unauthorized();

            if (request == null)
            {
                return BadRequest("Request payload cannot be null.");
            }

            try
            {
                var result = await _userService.ChangePassword(user.Id, request);
                if (!result.Success)
                {
                    return BadRequest(result.ErrorMessage);
                }

                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error changing password for user {UserId}", user.Id);
                return StatusCode(500, "An error occurred while changing the password.");
            }
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings([FromServices] WebApplication1.Repository.IUserRepository userRepository)
        {
            var user = (AuthenticatedUser?)HttpContext.Items["User"];
            if (user == null) return Unauthorized();

            var userDetails = await userRepository.GetUserById(user.Id);
            if (userDetails == null) return NotFound();

            return Ok(userDetails);
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request, [FromServices] WebApplication1.Repository.IUserRepository userRepository)
        {
            var user = (AuthenticatedUser?)HttpContext.Items["User"];
            if (user == null) return Unauthorized();

            var success = await userRepository.UpdateNotificationSettings(user.Id, request.EmailNotificationsEnabled, request.DiscordNotificationsEnabled);
            if (!success) return StatusCode(500, "Failed to update settings.");

            return Ok(new { message = "Settings updated successfully." });
        }

        [HttpGet("webhooks")]
        public async Task<IActionResult> GetWebhooks([FromServices] WebApplication1.Repository.IUserRepository userRepository)
        {
            var user = (AuthenticatedUser?)HttpContext.Items["User"];
            if (user == null) return Unauthorized();

            var webhooks = await userRepository.GetWebhooks(user.Id);
            return Ok(webhooks);
        }

        [HttpPost("webhooks")]
        public async Task<IActionResult> AddWebhook([FromBody] AddWebhookRequest request, [FromServices] WebApplication1.Repository.IUserRepository userRepository)
        {
            var user = (AuthenticatedUser?)HttpContext.Items["User"];
            if (user == null) return Unauthorized();

            // Validate webhook URL is a legitimate Discord webhook to prevent SSRF
            if (!Uri.TryCreate(request.WebhookUrl, UriKind.Absolute, out var webhookUri) ||
                webhookUri.Scheme != Uri.UriSchemeHttps ||
                !(string.Equals(webhookUri.Host, "discord.com", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(webhookUri.Host, "discordapp.com", StringComparison.OrdinalIgnoreCase)) ||
                !webhookUri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only Discord webhook URLs are supported (https://discord.com/api/webhooks/...).");
            }

            var existingWebhooks = await userRepository.GetWebhooks(user.Id);
            if (existingWebhooks.Count >= 5)
            {
                return BadRequest("Maximum limit of 5 Discord webhooks reached. Please delete an existing webhook before adding a new one.");
            }

            try
            {
                var webhook = await userRepository.AddWebhook(user.Id, request.Name, request.WebhookUrl);
                return Ok(webhook);
            }
            catch
            {
                return StatusCode(500, "Failed to add webhook.");
            }
        }

        [HttpDelete("webhooks/{id}")]
        public async Task<IActionResult> DeleteWebhook(Guid id, [FromServices] WebApplication1.Repository.IUserRepository userRepository)
        {
            var user = (AuthenticatedUser?)HttpContext.Items["User"];
            if (user == null) return Unauthorized();

            var success = await userRepository.DeleteWebhook(user.Id, id);
            if (!success) return NotFound("Webhook not found.");

            return Ok(new { message = "Webhook deleted successfully." });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAccount([FromServices] WebApplication1.Repository.IUserRepository userRepository)
        {
            var user = (AuthenticatedUser?)HttpContext.Items["User"];
            if (user == null) return Unauthorized();

            var success = await userRepository.DeleteUser(user.Id);
            if (!success) return StatusCode(500, "Failed to delete account.");

            // Clear session cookie
            Response.Cookies.Delete("sessionId", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            return Ok(new { message = "Account deleted successfully." });
        }
    }
}
