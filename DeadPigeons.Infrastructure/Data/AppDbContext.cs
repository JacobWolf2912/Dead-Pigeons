using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DeadPigeons.Core.Entities;

namespace DeadPigeons.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<PendingPlayer> PendingPlayers => Set<PendingPlayer>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Game> Games => Set<Game>();
        public DbSet<Board> Boards => Set<Board>();
        public DbSet<BoardNumber> BoardNumbers => Set<BoardNumber>();
        public DbSet<GameWinningNumbers> GameWinningNumbers => Set<GameWinningNumbers>();
        public DbSet<Transaction> Transactions => Set<Transaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Board>()
                .Property(b => b.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(10, 2);
        }
    }
}
