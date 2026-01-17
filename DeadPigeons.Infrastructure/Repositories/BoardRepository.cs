using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeadPigeons.Infrastructure.Repositories
{
    public class BoardRepository : IBoardRepository
    {
        private readonly AppDbContext _context;

        public BoardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Board> AddAsync(Board board)
        {
            _context.Boards.Add(board);
            await _context.SaveChangesAsync();
            return board;
        }

        public async Task<Board?> GetByIdAsync(Guid id)
        {
            return await _context.Boards
                .Include(b => b.Numbers)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Board>> GetByPlayerIdAsync(Guid playerId)
        {
            return await _context.Boards
                .Where(b => b.PlayerId == playerId)
                .Include(b => b.Numbers)
                .Include(b => b.Game)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Board>> GetByGameIdAsync(Guid gameId)
        {
            return await _context.Boards
                .Where(b => b.GameId == gameId)
                .Include(b => b.Numbers)
                .ToListAsync();
        }

        public async Task<IEnumerable<Board>> GetAllAsync()
        {
            return await _context.Boards
                .Include(b => b.Numbers)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Board board)
        {
            _context.Boards.Update(board);
            await _context.SaveChangesAsync();
        }
    }
}
