using Microsoft.AspNetCore.Identity;

namespace DeadPigeons.Core.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        // Additional user properties specific to Dead Pigeons
        public string FullName { get; set; } = null!;
        // Note: PhoneNumber is inherited from IdentityUser<Guid> - no need to redefine it
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property to Player if user is a player
        public Guid? PlayerId { get; set; }
        public Player? Player { get; set; }
    }
}
