using Microsoft.AspNetCore.Identity;
using DeadPigeons.Core.Entities;
using DeadPigeons.Infrastructure.Data;

namespace DeadPigeons.Infrastructure.Seeders
{
    public class DataSeeder
    {
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _dbContext;

        public DataSeeder(RoleManager<IdentityRole<Guid>> roleManager, UserManager<ApplicationUser> userManager, AppDbContext dbContext)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task SeedRolesAsync()
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
            }

            if (!await _roleManager.RoleExistsAsync("Player"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Player"));
            }
        }

        public async Task SeedAdminAsync()
        {
            const string adminEmail = "admin@deadpigeons.dk";
            const string adminPassword = "Pa55word.";

            // Check if admin already exists
            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin != null)
            {
                // Check if admin has Admin role
                var isAdmin = await _userManager.IsInRoleAsync(existingAdmin, "Admin");
                if (!isAdmin)
                {
                    Console.WriteLine("→ Assigning Admin role to existing account...");
                    await _userManager.AddToRoleAsync(existingAdmin, "Admin");
                    Console.WriteLine($"✓ Admin role assigned: {adminEmail}");
                }
                else
                {
                    Console.WriteLine($"✓ Admin account already exists with Admin role: {adminEmail}");
                }
                return;
            }

            try
            {
                Console.WriteLine("→ Creating admin account...");

                // Create Player record first
                var adminPlayer = new Player
                {
                    Id = Guid.NewGuid(),
                    FullName = "Admin User",
                    Email = adminEmail,
                    PhoneNumber = "40000000",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Players.Add(adminPlayer);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"✓ Player record created: {adminPlayer.Id}");

                // Create ApplicationUser (admin account)
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Admin User",
                    PhoneNumber = "40000000",
                    PlayerId = adminPlayer.Id
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    // Assign Admin role
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"✓ Admin account created successfully: {adminEmail}");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to create admin account. Errors:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error.Code}: {error.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error seeding admin account: {ex.Message}");
                Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            }
        }

        public async Task SeedInitialGameAsync()
        {
            // Check if any games exist
            var existingGames = _dbContext.Games.Count();
            if (existingGames > 0)
            {
                Console.WriteLine($"✓ Games already exist ({existingGames} game(s)). Skipping game seeding.");
                return;
            }

            try
            {
                Console.WriteLine("→ Creating initial game for this week...");

                // Get the current Saturday (or this Saturday if today is Saturday)
                TimeZoneInfo copenhagenZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                DateTime copenhagenNow = TimeZoneInfo.ConvertTime(DateTime.Now, copenhagenZone);

                int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)copenhagenNow.DayOfWeek + 7) % 7;
                if (daysUntilSaturday == 0)
                {
                    daysUntilSaturday = 0; // Today is Saturday
                }

                DateTime weekStart = copenhagenNow.AddDays(daysUntilSaturday).Date;
                DateTime drawTime = weekStart.AddHours(17); // 5 PM

                var game = new Game
                {
                    Id = Guid.NewGuid(),
                    WeekStart = weekStart,
                    DrawTime = drawTime,
                    IsClosed = false
                };

                _dbContext.Games.Add(game);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"✓ Initial game created: Week starting {weekStart:yyyy-MM-dd}, Draw at {drawTime:yyyy-MM-dd HH:mm}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error seeding initial game: {ex.Message}");
                Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            }
        }

        public async Task SetupTestGamesAsync()
        {
            try
            {
                // Check if there's already a closed test game (to avoid duplicates)
                var existingTestGame = _dbContext.Games
                    .Where(g => g.IsClosed && g.WeekStart < DateTime.UtcNow)
                    .FirstOrDefault();

                if (existingTestGame != null)
                {
                    Console.WriteLine($"✓ Test game already exists: {existingTestGame.Id}. Skipping test game creation.");
                    return;
                }

                // Create a new CLOSED game for testing the draw-numbers endpoint
                var testGame = new Game
                {
                    Id = Guid.NewGuid(),
                    WeekStart = DateTime.UtcNow.AddDays(-1),
                    DrawTime = DateTime.UtcNow.AddHours(-1), // Closed 1 hour ago, within 24-hour window
                    IsClosed = true, // Already closed, ready for number drawing
                    WinningNumbers = null,
                    Boards = new List<Board>()
                };

                _dbContext.Games.Add(testGame);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"✓ Test game created (CLOSED - ready for testing draw-numbers endpoint): {testGame.Id}");
                Console.WriteLine($"  - Game ID: {testGame.Id}");
                Console.WriteLine($"  - Status: CLOSED (can draw numbers now)");
                Console.WriteLine($"  - Hours until expiry: 23");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error seeding test game: {ex.Message}");
                Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            }
        }

        public async Task ReopenCurrentGameAsync()
        {
            try
            {
                // Find the most recent closed game and reopen it
                var closedGame = _dbContext.Games
                    .Where(g => g.IsClosed)
                    .OrderByDescending(g => g.WeekStart)
                    .FirstOrDefault();

                if (closedGame == null)
                {
                    Console.WriteLine("ℹ No closed games to reopen. Current game is already open.");
                    return;
                }

                // Reopen the game
                closedGame.IsClosed = false;
                closedGame.WinningNumbers = null;
                _dbContext.Games.Update(closedGame);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"✓ Game reopened: {closedGame.Id}");
                Console.WriteLine($"  - Status: OPEN (players can purchase boards)");
                Console.WriteLine($"  - Boards in game: {closedGame.Boards?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reopening game: {ex.Message}");
                Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            }
        }

        public async Task EnsureSingleOpenGameAsync()
        {
            try
            {
                // Find all open games (not closed, no winning numbers)
                var openGames = _dbContext.Games
                    .Where(g => !g.IsClosed && g.WinningNumbers == null)
                    .OrderByDescending(g => g.WeekStart)
                    .ToList();

                if (openGames.Count == 0)
                {
                    // No open game exists, create one for the current/next Saturday
                    Console.WriteLine("→ No open games found. Creating new open game...");

                    TimeZoneInfo copenhagenZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                    DateTime copenhagenNow = TimeZoneInfo.ConvertTime(DateTime.Now, copenhagenZone);

                    int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)copenhagenNow.DayOfWeek + 7) % 7;
                    if (daysUntilSaturday == 0 && copenhagenNow.Hour >= 17)
                    {
                        // Already past 5 PM on Saturday, get next Saturday
                        daysUntilSaturday = 7;
                    }
                    else if (daysUntilSaturday == 0)
                    {
                        // Today is Saturday before 5 PM, use today
                        daysUntilSaturday = 0;
                    }

                    DateTime weekStart = copenhagenNow.AddDays(daysUntilSaturday).Date;
                    DateTime drawTime = weekStart.AddHours(17);

                    var newGame = new Game
                    {
                        Id = Guid.NewGuid(),
                        WeekStart = weekStart,
                        DrawTime = drawTime,
                        IsClosed = false
                    };

                    _dbContext.Games.Add(newGame);
                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"✓ New open game created: Week starting {weekStart:yyyy-MM-dd}, Draw at {drawTime:yyyy-MM-dd HH:mm}");
                }
                else if (openGames.Count > 1)
                {
                    // Multiple open games exist - keep the most recent, close the others
                    Console.WriteLine($"⚠ Found {openGames.Count} open games. Closing all but the most recent...");

                    var mostRecentGame = openGames.First();
                    var gamesToClose = openGames.Skip(1).ToList();

                    foreach (var game in gamesToClose)
                    {
                        game.IsClosed = true;
                        _dbContext.Games.Update(game);
                        Console.WriteLine($"  - Closed game {game.Id} (Week starting {game.WeekStart:yyyy-MM-dd})");
                    }

                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"✓ Duplicate games closed. Current active game: {mostRecentGame.Id}");
                }
                else
                {
                    // Exactly one open game exists - perfect!
                    Console.WriteLine($"✓ Single open game confirmed: {openGames[0].Id} (Week starting {openGames[0].WeekStart:yyyy-MM-dd})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error ensuring single open game: {ex.Message}");
                Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            }
        }

        public async Task CleanupNonAdminPlayersAsync()
        {
            try
            {
                // Get all non-admin users (admin account has Admin role)
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                var adminPlayerIds = adminUsers
                    .Where(u => u.PlayerId.HasValue)
                    .Select(u => u.PlayerId!.Value)
                    .ToList();

                // Find all players that are NOT associated with admin accounts
                var nonAdminPlayers = _dbContext.Players
                    .Where(p => !adminPlayerIds.Contains(p.Id))
                    .ToList();

                if (nonAdminPlayers.Count == 0)
                {
                    Console.WriteLine("✓ No non-admin players to clean up.");
                    return;
                }

                // Get corresponding ApplicationUsers and delete them first (to avoid foreign key issues)
                var nonAdminPlayerIds = nonAdminPlayers.Select(p => p.Id).ToList();
                var usersToDelete = _dbContext.Users
                    .Where(u => u.PlayerId.HasValue && nonAdminPlayerIds.Contains(u.PlayerId.Value))
                    .ToList();

                // Delete the ApplicationUsers
                foreach (var user in usersToDelete)
                {
                    await _userManager.DeleteAsync(user);
                }

                // Delete the Players
                _dbContext.Players.RemoveRange(nonAdminPlayers);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"✓ Cleaned up {nonAdminPlayers.Count} non-admin player(s)");
                Console.WriteLine("  Players removed:");
                foreach (var player in nonAdminPlayers)
                {
                    Console.WriteLine($"    - {player.FullName} ({player.Email})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error cleaning up players: {ex.Message}");
                Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            }
        }

        public async Task CleanupAllPendingPlayersAsync()
        {
            try
            {
                // Get all pending players
                var pendingPlayers = _dbContext.PendingPlayers.ToList();

                if (pendingPlayers.Count == 0)
                {
                    Console.WriteLine("✓ No pending players to clean up.");
                    return;
                }

                // Delete all pending players
                _dbContext.PendingPlayers.RemoveRange(pendingPlayers);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"✓ Cleaned up {pendingPlayers.Count} pending player(s)");
                Console.WriteLine("  Pending players removed:");
                foreach (var player in pendingPlayers)
                {
                    Console.WriteLine($"    - {player.FullName} ({player.Email})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error cleaning up pending players: {ex.Message}");
                Console.WriteLine($"✗ Stack trace: {ex.StackTrace}");
            }
        }
    }
}