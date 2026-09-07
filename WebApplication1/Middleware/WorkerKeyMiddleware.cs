using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Middleware
{
    /// <summary>
    /// Authenticates internal API requests from the Cloudflare Worker
    /// using a shared secret in the X-Worker-Key header.
    /// Only applied to /api/internal/* routes.
    /// </summary>
    public class WorkerKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly byte[] _workerKeyBytes;

        public WorkerKeyMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            var workerKey = config["WORKER_KEY"]
                ?? throw new InvalidOperationException("WORKER_KEY is not configured.");
            _workerKeyBytes = Encoding.UTF8.GetBytes(workerKey);
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // Only enforce on internal API routes
            if (!path.StartsWith("/api/internal/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var providedKey = context.Request.Headers["X-Worker-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(providedKey) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(providedKey), _workerKeyBytes))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized.");
                return;
            }

            await _next(context);
        }
    }
}
