using WebApplication1.Repository;
using WebApplication1.services;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

var apiKey = builder.Configuration["Steam:ApiKey"];

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISteamRepository, SteamRepository>();
builder.Services.AddScoped<ISteamService, SteamService>();
builder.Services.AddHttpClient<ISteamService, SteamService>();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
