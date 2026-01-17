using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeadPigeons.Infrastructure.Services
{
    public class GameService : IGameService
    {
        private readonly AppDbContext _context;
        private readonly IBoardService _boardService;

        public GameService(AppDbContext context, IBoardService boardService)
        {
            _context = context;
            _boardService = boardService;
        }

        // Get all games
        public async Task<IEnumerable<Game>> GetAllGamesAsync()
        {
            return await _context.Games
                .Include(g => g.WinningNumbers)
                .Include(g => g.Boards)
                .OrderByDescending(g => g.WeekStart)
                .ToListAsync();
        }

        // Get a specific game
        public async Task<Game?> GetGameAsync(Guid gameId)
        {
            return await _context.Games
                .Include(g => g.WinningNumbers)
                .Include(g => g.Boards)
                .FirstOrDefaultAsync(g => g.Id == gameId);
        }

        // Get the current active game (not closed, has no winning numbers yet)
        public async Task<Game?> GetCurrentGameAsync()
        {
            return await _context.Games
                .Where(g => !g.IsClosed && g.WinningNumbers == null)
                .Include(g => g.Boards)
                .FirstOrDefaultAsync();
        }

        // Admin draws winning numbers for a game
        public async Task<Game> DrawWinningNumbersAsync(Guid gameId, int number1, int number2, int number3)
        {
            // Validate numbers are between 1-16
            var numbers = new[] { number1, number2, number3 };
            if (numbers.Any(n => n < 1 || n > 16))
            {
                throw new ArgumentException("All winning numbers must be between 1 and 16");
            }

            // Get the game
            var game = await _context.Games
                .Include(g => g.Boards)
                .ThenInclude(b => b.Numbers)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
            {
                throw new Exception("Game not found");
            }

            if (game.IsClosed)
            {
                throw new InvalidOperationException("Game is already closed");
            }

            if (game.WinningNumbers != null)
            {
                throw new InvalidOperationException("Winning numbers already drawn for this game");
            }

            // Create winning numbers record
            var winningNumbers = new GameWinningNumbers
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                Number1 = number1,
                Number2 = number2,
                Number3 = number3,
                DrawnAt = DateTime.UtcNow
            };

            // Add winning numbers to context and save first
            _context.Add(winningNumbers);

            // Determine winning boards and mark them
            if (game.Boards != null && game.Boards.Count > 0)
            {
                foreach (var board in game.Boards)
                {
                    var isWinning = _boardService.IsWinningBoard(board, winningNumbers);
                    board.IsWinningBoard = isWinning; // Mark as winning or not winning
                }
            }

            // Close game
            game.IsClosed = true;
            game.WinningNumbers = winningNumbers;

            // Save all changes (boards are tracked from Include, so they'll be saved automatically)
            await _context.SaveChangesAsync();

            return game;
        }

        // Get all winning boards for a game
        public async Task<IEnumerable<Board>> GetWinningBoardsForGameAsync(Guid gameId)
        {
            return await _context.Boards
                .Where(b => b.GameId == gameId && b.IsWinningBoard)
                .Include(b => b.Numbers)
                .ToListAsync();
        }

        // Activate the next game (should be pre-seeded)
        // This is a stateless approach - games are pre-created, we just activate the next one
        public async Task<Game?> ActivateNextGameAsync()
        {
            // Find the first inactive, unstarted game
            var nextGame = await _context.Games
                .Where(g => g.WinningNumbers == null && !g.IsClosed)
                .OrderBy(g => g.WeekStart)
                .FirstOrDefaultAsync();

            // If no game found, we've run out of pre-seeded games
            if (nextGame == null)
            {
                return null;
            }

            // The next game is already active (no need to change state)
            return nextGame;
        }
    }
}
