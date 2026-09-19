using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Repository;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SteamController : ControllerBase
    {
        private readonly ISteamService _steamService;
        private readonly ISteamRepository _steamRepository;
        private readonly ILogger<SteamController> _logger;

        public SteamController(ISteamService steamService, ISteamRepository steamRepository, ILogger<SteamController> logger)
        {
            _steamService = steamService;
            _steamRepository = steamRepository;
            _logger = logger;
        }

        // Get all Steam accounts tracked by the authenticated user
        [HttpGet("tracked")]
        public async Task<IActionResult> GetTrackedAccounts()
        {
            var user = HttpContext.Items["User"] as AuthenticatedUser;
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                var accounts = await _steamRepository.GetTrackedAccountsByUser(user.Id.ToString());
                return Ok(new { accounts });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to fetch tracked accounts for user {UserId}", user.Id);
                return StatusCode(500, "An error occurred while fetching tracked accounts.");
            }
        }

        // Track a Steam account. Accepts any format: SteamID64, legacy STEAM_ID,
        // vanity URL, or full profile URL.
        [HttpPost("track")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> TrackAccount([FromBody] TrackSteamRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SteamInput))
            {
                return BadRequest("A Steam ID, profile URL, or vanity URL is required.");
            }

            // Get the authenticated user from the middleware
            var user = HttpContext.Items["User"] as AuthenticatedUser;
            if (user == null)
            {
                return Unauthorized();
            }

            // Resolve whatever input format into a SteamID64
            var steamId64 = await _steamService.ResolveSteamID64(request.SteamInput.Trim());
            if (steamId64 == null)
            {
                return BadRequest("Could not resolve the provided input to a valid Steam account. Accepted formats: SteamID64, STEAM_X:Y:Z, vanity URL, or profile URL.");
            }

            try
            {
                var isNewlyTracked = await _steamRepository.TrackSteamAccount(user.Id.ToString(), steamId64);
                
                if (!isNewlyTracked)
                {
                    return Conflict(new { message = "You are already tracking this Steam account." });
                }

                return Ok(new { message = "Steam account is now being tracked.", steamId64 });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to track Steam account {SteamId64} for user {UserId}", steamId64, user.Id);
                return StatusCode(500, "An error occurred while tracking the Steam account.");
            }
        }

        // Untrack a Steam account by its SteamID64
        [HttpDelete("track/{steamId64}")]
        public async Task<IActionResult> UntrackAccount(string steamId64)
        {
            if (string.IsNullOrWhiteSpace(steamId64))
            {
                return BadRequest("SteamID64 is required.");
            }

            var user = HttpContext.Items["User"] as AuthenticatedUser;
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                var removed = await _steamRepository.DeleteTrackedAccount(user.Id.ToString(), steamId64.Trim());
                if (!removed)
                {
                    return NotFound(new { message = "Account was not being tracked by this user." });
                }

                return Ok(new { message = "Account untracked successfully." });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to untrack Steam account {SteamId64} for user {UserId}", steamId64, user.Id);
                return StatusCode(500, "An error occurred while untracking the Steam account.");
            }
        }
    }
}
