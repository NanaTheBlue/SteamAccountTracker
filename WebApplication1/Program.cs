using System.Threading.RateLimiting;
using WebApplication1.Middleware;
using WebApplication1.Repository;
using WebApplication1.Services;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISteamRepository, SteamRepository>();

builder.Services.AddHttpClient<ISteamService, SteamService>();

// Health Checks — monitor API and Database health
builder.Services.AddHealthChecks()
    .AddAsyncCheck("database", async () =>
    {
        var connectionString = builder.Configuration["CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Connection string is not configured.");
        }
        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync();
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(ex.Message);
        }
    });

// Request/Response logging for observability
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode
                          | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Duration;
});

// Rate limiting — protect login/register from brute-force
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// CORS — restrict to your frontend origin
var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "https://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseRateLimiter();

// Health check endpoint (probed by orchestrators / load balancers)
app.MapHealthChecks("/healthz");

// WorkerKeyMiddleware must come before AuthMiddleware so internal
// API routes are authenticated by shared secret, not session cookie
app.UseMiddleware<WorkerKeyMiddleware>();

app.UseMiddleware<AuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Automatically apply database migrations on startup if connection string is present
var connectionString = builder.Configuration.GetConnectionString("CONNECTION_STRING")
    ?? builder.Configuration["CONNECTION_STRING"];

if (!string.IsNullOrWhiteSpace(connectionString))
{
    try
    {
        await WebApplication1.Migrations.MigrationRunner.ApplyMigrationsAsync(connectionString, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not apply database migrations on startup. Ensure SQL Server is reachable.");
    }
}

app.Run();
