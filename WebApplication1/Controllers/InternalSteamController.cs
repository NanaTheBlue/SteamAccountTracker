using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dtos;
using WebApplication1.Repository;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Internal API consumed by the Cloudflare Worker ban scanner.
    /// Secured via WorkerKeyMiddleware (X-Worker-Key header), not session auth.
    /// </summary>
    [ApiController]
    [Route("api/internal/steam")]
    public class InternalSteamController : ControllerBase
    {
        private readonly ISteamRepository _steamRepository;
        private readonly ILogger<InternalSteamController> _logger;

        public InternalSteamController(ISteamRepository steamRepository, ILogger<InternalSteamController> logger)
        {
            _steamRepository = steamRepository;
            _logger = logger;
        }

        /// <summary>
        /// Returns tracked Steam accounts with their current ban state,
        /// ordered by LastScannedAt ASC (oldest / never-scanned first).
        /// Paginated — use offset and limit query params.
        /// </summary>
        [HttpGet("accounts-to-scan")]
        public async Task<IActionResult> GetAccountsToScan([FromQuery] int offset = 0, [FromQuery] int limit = 1000)
        {
            if (offset < 0)
            {
                return BadRequest("Offset must be non-negative.");
            }
            if (limit <= 0)
            {
                return BadRequest("Limit must be greater than zero.");
            }

            // Cap limit to prevent abuse
            limit = Math.Min(limit, 5000);

            try
            {
                var accounts = await _steamRepository.GetAllTrackedAccounts(offset, limit);
                return Ok(new { accounts, offset, limit, count = accounts.Count });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get accounts to scan");
                return StatusCode(500, "An error occurred while fetching accounts.");
            }
        }

        /// <summary>
        /// Receives ban status updates from the worker, persists them,
        /// and returns the list of user emails to notify.
        /// </summary>
        [HttpPost("ban-updates")]
        public async Task<IActionResult> PostBanUpdates([FromBody] BanUpdateRequest request)
        {
            if (request?.Updates == null || request.Updates.Count == 0)
            {
                return BadRequest("No updates provided.");
            }

            // Validate individual update items
            var validUpdates = request.Updates
                .Where(u => !string.IsNullOrWhiteSpace(u.SteamId64) && u.SteamId64.Trim().Length == 17 && u.SteamId64.Trim().All(char.IsDigit))
                .ToList();

            if (validUpdates.Count == 0)
            {
                return BadRequest("No valid updates provided. Each entry must have a valid 17-digit numeric SteamId64.");
            }

            try
            {
                var notifications = await _steamRepository.UpdateBanStatusAndGetNotifications(validUpdates);

                _logger.LogInformation(
                    "Processed {UpdateCount} ban updates ({ValidCount} valid), {NotificationCount} notifications to send",
                    request.Updates.Count, validUpdates.Count, notifications.Count);

                return Ok(new BanUpdateResponse { Notifications = notifications });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to process ban updates");
                return StatusCode(500, "An error occurred while processing ban updates.");
            }
        }

        /// <summary>
        /// Updates the LastScannedAt timestamp for accounts that were successfully checked,
        /// ensuring they cycle to the back of the scanning queue.
        /// </summary>
        [HttpPost("mark-scanned")]
        public async Task<IActionResult> MarkScanned([FromBody] List<string> steamId64s)
        {
            if (steamId64s == null || steamId64s.Count == 0)
            {
                return BadRequest("No Steam IDs provided.");
            }

            try
            {
                await _steamRepository.UpdateLastScannedAt(steamId64s);
                return Ok(new { message = $"Updated LastScannedAt for {steamId64s.Count} accounts." });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to mark accounts as scanned");
                return StatusCode(500, "An error occurred while updating scanned timestamps.");
            }
        }
    }
}
