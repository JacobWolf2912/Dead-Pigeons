using DeadPigeons.Core.Entities;

namespace DeadPigeons.Core.Interfaces
{
    public interface IGameService
    {
        // Get all games
        Task<IEnumerable<Game>> GetAllGamesAsync();

        // Get a specific game
        Task<Game?> GetGameAsync(Guid gameId);

        // Get the current active game
        Task<Game?> GetCurrentGameAsync();

        // Admin draws winning numbers for a game and marks it closed
        Task<Game> DrawWinningNumbersAsync(Guid gameId, int number1, int number2, int number3);

        // Get all winning boards for a game
        Task<IEnumerable<Board>> GetWinningBoardsForGameAsync(Guid gameId);

        // Activate the next game (should be seeded beforehand)
        Task<Game?> ActivateNextGameAsync();
    }
}
