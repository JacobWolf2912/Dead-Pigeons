using DeadPigeons.Core.Entities;

namespace DeadPigeons.Core.Interfaces
{
    public interface ITransactionService
    {
        // Create a new deposit transaction (pending by default)
        Task<Transaction> CreateDepositAsync(Guid playerId, decimal amount, string mobilePayId);

        // Get all transactions for a player
        Task<IEnumerable<Transaction>> GetPlayerTransactionsAsync(Guid playerId);

        // Calculate player's current balance (approved deposits - board purchases)
        Task<decimal> GetPlayerBalanceAsync(Guid playerId);

        // Approve a pending transaction
        Task<Transaction> ApproveTransactionAsync(Guid transactionId);

        // Get all pending transactions (for admin to review)
        Task<IEnumerable<Transaction>> GetPendingTransactionsAsync();

        // Get a specific transaction
        Task<Transaction?> GetTransactionAsync(Guid transactionId);
    }
}
