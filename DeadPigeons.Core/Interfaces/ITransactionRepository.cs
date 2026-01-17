using DeadPigeons.Core.Entities;

namespace DeadPigeons.Core.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction> AddAsync(Transaction transaction);
        Task<Transaction?> GetByIdAsync(Guid id);
        Task<IEnumerable<Transaction>> GetByPlayerIdAsync(Guid playerId);
        Task<IEnumerable<Transaction>> GetAllAsync();
        Task<IEnumerable<Transaction>> GetApprovedTransactionsByPlayerIdAsync(Guid playerId);
        Task UpdateAsync(Transaction transaction);
    }
}
