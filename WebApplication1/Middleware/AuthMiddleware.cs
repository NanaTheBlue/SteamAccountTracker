using WebApplication1.Services;

namespace WebApplication1.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        // Endpoints that don't require authentication
        private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/user/login",
            "/api/user/register",
            "/healthz"
        };

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUserService userService, ILogger<AuthMiddleware> logger)
        {
            var path = context.Request.Path.Value ?? "";

            // Skip auth for public endpoints, Swagger, and internal worker API
            if (PublicPaths.Contains(path) ||
                path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/internal/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var sessionCookie = context.Request.Cookies["sessionId"];
            if (string.IsNullOrEmpty(sessionCookie))
            {
                logger.LogWarning("Unauthorized access to {Path} from {IP}: No token provided.", path, context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: No token provided.");
                return;
            }

            if (!Guid.TryParse(sessionCookie, out var sessionId))
            {
                logger.LogWarning("Unauthorized access to {Path} from {IP}: Invalid token format.", path, context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid token.");
                return;
            }

            var user = await userService.GetUserFromSession(sessionId);
            if (user == null)
            {
                logger.LogWarning("Unauthorized access to {Path} from {IP}: Invalid or expired session.", path, context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid or expired session.");
                return;
            }

            context.Items["User"] = user;

            await _next(context);
        }
    }
}
