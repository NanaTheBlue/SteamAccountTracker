using System.Threading.RateLimiting;
using WebApplication1.Middleware;
using WebApplication1.Repository;
using WebApplication1.services;
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseRateLimiter();

// WorkerKeyMiddleware must come before AuthMiddleware so internal
// API routes are authenticated by shared secret, not session cookie
app.UseMiddleware<WorkerKeyMiddleware>();

app.UseMiddleware<AuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
