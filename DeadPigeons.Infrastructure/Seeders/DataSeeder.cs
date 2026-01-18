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
    }
}