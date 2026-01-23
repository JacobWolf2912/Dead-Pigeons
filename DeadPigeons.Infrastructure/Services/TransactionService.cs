using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeadPigeons.Infrastructure.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly AppDbContext _context;

        public TransactionService(ITransactionRepository transactionRepository, AppDbContext context)
        {
            _transactionRepository = transactionRepository;
            _context = context;
        }

        // Create a new deposit transaction (starts as pending)
        public async Task<Transaction> CreateDepositAsync(Guid playerId, decimal amount, string mobilePayId)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                Amount = amount,
                MobilePayTransactionId = mobilePayId,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false // Starts as pending
            };

            return await _transactionRepository.AddAsync(transaction);
        }

        // Calculate player's balance
        // Balance = Sum of all approved transactions - Sum of all board purchases
        // This way, balance is always verifiable from the transaction history
        public async Task<decimal> GetPlayerBalanceAsync(Guid playerId)
        {
            // Get sum of all approved deposits for this player
            var approvedTransactions = await _context.Transactions
                .Where(t => t.PlayerId == playerId && t.IsApproved && !t.IsDeleted)
                .SumAsync(t => t.Amount);

            // Get sum of all board purchases for this player
            var boardPurchases = await _context.Boards
                .Where(b => b.PlayerId == playerId && !b.IsDeleted)
                .SumAsync(b => b.Price);

            // Balance is deposits minus purchases
            var balance = approvedTransactions - boardPurchases;

            // Balance can never be negative (but we check this before allowing purchases)
            return Math.Max(0, balance);
        }

        // Get all transactions for a specific player
        public async Task<IEnumerable<Transaction>> GetPlayerTransactionsAsync(Guid playerId)
        {
            return await _transactionRepository.GetByPlayerIdAsync(playerId);
        }

        // Approve a pending transaction (optionally with edited amount)
        public async Task<Transaction> ApproveTransactionAsync(Guid transactionId, decimal? approveAmount = null)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new Exception("Transaction not found");
            }

            transaction.IsApproved = true;
            transaction.ApprovedAt = DateTime.UtcNow;

            // Update amount if provided (admin edited it)
            if (approveAmount.HasValue)
            {
                transaction.Amount = approveAmount.Value;
            }

            await _transactionRepository.UpdateAsync(transaction);
            return transaction;
        }

        // Delete a pending transaction (dismiss it)
        public async Task DeleteTransactionAsync(Guid transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new Exception("Transaction not found");
            }

            // Only allow deletion of pending transactions
            if (transaction.IsApproved)
            {
                throw new Exception("Cannot delete an approved transaction");
            }

            await _transactionRepository.DeleteAsync(transactionId);
        }

        // Get all pending transactions (for admin review)
        public async Task<IEnumerable<Transaction>> GetPendingTransactionsAsync()
        {
            return await _context.Transactions
                .Where(t => !t.IsApproved && !t.IsDeleted)
                .Include(t => t.Player)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        // Get a specific transaction
        public async Task<Transaction?> GetTransactionAsync(Guid transactionId)
        {
            return await _transactionRepository.GetByIdAsync(transactionId);
        }
    }
}
