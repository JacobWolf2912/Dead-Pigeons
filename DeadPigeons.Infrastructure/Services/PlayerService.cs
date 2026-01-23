using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeadPigeons.Infrastructure.Services;

public class PlayerService : IPlayerService
{
    private readonly AppDbContext _context;

    public PlayerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Player> CreateAsync(Player player)
    {
        // Only generate a new ID if one wasn't provided
        if (player.Id == Guid.Empty)
        {
            player.Id = Guid.NewGuid();
        }

        // Only set CreatedAt if not already set
        if (player.CreatedAt == default)
        {
            player.CreatedAt = DateTime.UtcNow;
        }

        // Don't override IsActive if explicitly set to true
        if (!player.IsActive)
        {
            player.IsActive = false;
        }

        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        return player;
    }

    public async Task<Player?> GetByIdAsync(Guid id)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<IEnumerable<Player>> GetAllAsync()
    {
        return await _context.Players.Where(p => !p.IsDeleted).AsNoTracking().ToListAsync();
    }

    public async Task UpdateAsync(Player player)
    {
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
    }

    public async Task SetActiveStatusAsync(Guid playerId, bool isActive)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId && !p.IsDeleted);

        if (player == null)
            throw new InvalidOperationException("Player not found");

        player.IsActive = isActive;
        await _context.SaveChangesAsync();
    }
}