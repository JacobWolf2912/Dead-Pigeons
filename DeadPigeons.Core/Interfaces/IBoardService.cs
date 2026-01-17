using DeadPigeons.Core.Entities;

namespace DeadPigeons.Core.Interfaces
{
    public interface IBoardService
    {
        // Get pricing for a board based on field count
        decimal GetBoardPrice(int fieldCount);

        // Purchase a board - checks balance and deducts from it
        Task<Board> PurchaseBoardAsync(Guid playerId, Guid gameId, int fieldCount, List<int> numbers);

        // Get all boards for a player
        Task<IEnumerable<Board>> GetPlayerBoardsAsync(Guid playerId);

        // Get all boards for a game
        Task<IEnumerable<Board>> GetGameBoardsAsync(Guid gameId);

        // Get a specific board
        Task<Board?> GetBoardAsync(Guid boardId);

        // Check if board is winning (contains all winning numbers)
        bool IsWinningBoard(Board board, GameWinningNumbers winningNumbers);
    }
}
