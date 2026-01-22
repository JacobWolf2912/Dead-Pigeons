using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using DeadPigeons.Infrastructure.Repositories;
using DeadPigeons.Infrastructure.Seeders;
using DeadPigeons.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add CORS - Allow frontend to communicate with backend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow localhost for development
            policy.WithOrigins("http://localhost:3000", "http://localhost:5047")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            // Allow Vercel production deployment and any vercel.app subdomain
            policy.SetIsOriginAllowed(origin =>
                origin == "https://dead-pigeons-e9z2tfcct-jakub-mazurs-projects.vercel.app" ||
                origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        }
    });
});

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
    b => b.MigrationsAssembly("DeadPigeons.Infrastructure")));

// Add Identity - handles user authentication and password hashing
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSecret = "your-super-secret-jwt-key-change-this-in-production-at-least-32-characters-long";
var jwtIssuer = "DeadPigeonsAPI";
var jwtAudience = "DeadPigeonsApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// Store JWT settings for use in controllers
builder.Services.AddSingleton(new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience
});

// Add repositories and services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register repositories and services
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<DataSeeder>();

// Register background service for weekly game scheduling
builder.Services.AddHostedService<GameSchedulerService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedRolesAsync();
    await seeder.SeedAdminAsync();
    await seeder.CleanupAllPendingPlayersAsync();  // Clean up all pending players
    await seeder.CleanupNonAdminPlayersAsync();  // Clean up any old test/non-admin players
    await seeder.SeedInitialGameAsync();
    await seeder.ReopenCurrentGameAsync();  // Reopen the current game
    await seeder.SetupTestGamesAsync();     // Create a closed test game
    await seeder.EnsureSingleOpenGameAsync(); // Ensure only one open game exists at a time
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS middleware - Must come before authentication
app.UseCors("AllowReactFrontend");

// Authentication & Authorization middleware - ORDER MATTERS!
// UseAuthentication must come before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
