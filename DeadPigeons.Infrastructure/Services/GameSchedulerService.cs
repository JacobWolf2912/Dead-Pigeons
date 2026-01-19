using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace DeadPigeons.Infrastructure.Services
{
    /// <summary>
    /// Background service that runs every Saturday at 17:00 (5 PM) Danish time.
    /// Closes the current game by drawing random winning numbers and creates a new game for next week.
    /// </summary>
    public class GameSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public GameSchedulerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate time until next Saturday 17:00 Copenhagen time
                    var timeUntilNextRun = GetTimeUntilNextSaturdayFivepm();

                    // Wait until it's time to run
                    await Task.Delay(timeUntilNextRun, stoppingToken);

                    // Run the weekly cycle
                    await RunWeeklyCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Application is shutting down
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GameSchedulerService: {ex.Message}");
                    // Wait 5 minutes before trying again if there's an error
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private TimeSpan GetTimeUntilNextSaturdayFivepm()
        {
            // Get current time in Copenhagen timezone (UTC+1 or UTC+2 with DST)
            TimeZoneInfo copenhagenZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            DateTime copenhagenNow = TimeZoneInfo.ConvertTime(DateTime.Now, copenhagenZone);

            // Saturday = 6 in DayOfWeek enum
            int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)copenhagenNow.DayOfWeek + 7) % 7;

            // If it's already Saturday, check if it's before 17:00
            if (daysUntilSaturday == 0 && copenhagenNow.Hour >= 17)
            {
                daysUntilSaturday = 7; // Run next Saturday instead
            }

            // Create target datetime (Saturday at 17:00)
            DateTime targetDate = copenhagenNow.AddDays(daysUntilSaturday);
            DateTime target = new DateTime(
                targetDate.Year,
                targetDate.Month,
                targetDate.Day,
                17, // 5 PM
                0,
                0
            );

            TimeSpan timeUntilRun = target - copenhagenNow;

            // Ensure it's positive
            if (timeUntilRun.TotalMilliseconds <= 0)
            {
                timeUntilRun = timeUntilRun.Add(TimeSpan.FromDays(7));
            }

            return timeUntilRun;
        }

        private async Task RunWeeklyCycleAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

                try
                {
                    // Get the current active game
                    var currentGame = await gameService.GetCurrentGameAsync();

                    if (currentGame != null && !currentGame.IsClosed)
                    {
                        // Close the game (admin will input numbers within 24 hours)
                        currentGame.IsClosed = true;
                        context.Games.Update(currentGame);
                        await context.SaveChangesAsync(stoppingToken);

                        Console.WriteLine($"Game {currentGame.Id} closed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. Admin has 24 hours to input winning numbers.");
                    }

                    // Create a new game for next week if none exists
                    var nextGame = await gameService.ActivateNextGameAsync();
                    if (nextGame == null)
                    {
                        // No pre-seeded game exists, create one
                        var newGame = new Game
                        {
                            Id = Guid.NewGuid(),
                            WeekStart = GetNextSaturdayDate(),
                            DrawTime = GetNextSaturdayDate().AddHours(17),
                            IsClosed = false
                        };

                        context.Games.Add(newGame);
                        await context.SaveChangesAsync(stoppingToken);

                        Console.WriteLine($"New game created for week starting {newGame.WeekStart}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in weekly cycle: {ex.Message}");
                    throw;
                }
            }
        }

        private DateTime GetNextSaturdayDate()
        {
            TimeZoneInfo copenhagenZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            DateTime copenhagenNow = TimeZoneInfo.ConvertTime(DateTime.Now, copenhagenZone);

            int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)copenhagenNow.DayOfWeek + 7) % 7;
            if (daysUntilSaturday == 0)
            {
                daysUntilSaturday = 7; // Get next Saturday
            }

            return copenhagenNow.AddDays(daysUntilSaturday).Date;
        }
    }
}
