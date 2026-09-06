using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                MaxAge = TimeSpan.FromHours(24),
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
    }
}
