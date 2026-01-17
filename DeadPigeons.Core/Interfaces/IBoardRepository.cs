using DeadPigeons.Core.Entities;

namespace DeadPigeons.Core.Interfaces
{
    public interface IBoardRepository
    {
        Task<Board> AddAsync(Board board);
        Task<Board?> GetByIdAsync(Guid id);
        Task<IEnumerable<Board>> GetByPlayerIdAsync(Guid playerId);
        Task<IEnumerable<Board>> GetByGameIdAsync(Guid gameId);
        Task<IEnumerable<Board>> GetAllAsync();
        Task UpdateAsync(Board board);
    }
}
