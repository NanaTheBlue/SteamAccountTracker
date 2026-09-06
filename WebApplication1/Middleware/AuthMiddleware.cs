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
            "/api/user/register"
        };

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUserService userService)
        {
            var path = context.Request.Path.Value ?? "";

            // Skip auth for public endpoints and Swagger
            if (PublicPaths.Contains(path) ||
                path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var sessionCookie = context.Request.Cookies["sessionId"];
            if (string.IsNullOrEmpty(sessionCookie))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: No token provided.");
                return;
            }

            if (!Guid.TryParse(sessionCookie, out var sessionId))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid token.");
                return;
            }

            var user = await userService.GetUserFromSession(sessionId);
            if (user == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid or expired session.");
                return;
            }

            context.Items["User"] = user;

            await _next(context);
        }
    }
}
