using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Repository;
using WebApplication1.services;

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

        // Track a Steam account. Accepts any format: SteamID64, legacy STEAM_ID,
        // vanity URL, or full profile URL.
        [HttpPost("track")]
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
                await _steamRepository.TrackSteamAccount(user.Id.ToString(), steamId64);
                return Ok(new { message = "Steam account is now being tracked.", steamId64 });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to track Steam account {SteamId64} for user {UserId}", steamId64, user.Id);
                return StatusCode(500, "An error occurred while tracking the Steam account.");
            }
        }
    }

    public class TrackSteamRequest
    {
    
        // Any Steam identifier: SteamID64, legacy STEAM_ID (STEAM_X:Y:Z),
        // vanity URL (https://steamcommunity.com/id/name), or profile URL
        // (https://steamcommunity.com/profiles/12345).
    
        public required string SteamInput { get; set; }
    }
}

