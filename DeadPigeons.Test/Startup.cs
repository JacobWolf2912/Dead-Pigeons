using DeadPigeons.Infrastructure.Data;
using DeadPigeons.Infrastructure.Repositories;
using DeadPigeons.Infrastructure.Services;
using DeadPigeons.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace DeadPigeons.Test;

public class Startup
{
    private static MsSqlContainer? _container;

    public async void ConfigureServices(IServiceCollection services)
    {
        // Start the TestContainer SQL Server once
        if (_container == null)
        {
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

            await _container.StartAsync();
        }

        // Register DbContext with the container's connection string
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(_container.GetConnectionString())
        );

        // Register repositories
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        // Register services
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<ITransactionService, TransactionService>();

        // Initialize database schema
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();
    }
}
