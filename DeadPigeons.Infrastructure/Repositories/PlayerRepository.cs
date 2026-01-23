using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeadPigeons.Infrastructure.Repositories
{
    public class PlayerRepository: IPlayerRepository
    {
        private readonly AppDbContext _context;
        public PlayerRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Player> AddAsync(Player player)
        { 
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
            return await _context.Players.Where(p => !p.IsDeleted).ToListAsync();
        }

        public async Task UpdateAsync(Player player)
        {
            _context.Players.Update(player);
            await _context.SaveChangesAsync();
        }
    }
}
