using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeadPigeons.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _context;

        public TransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction> AddAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            return await _context.Transactions
                .Include(t => t.Player)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        public async Task<IEnumerable<Transaction>> GetByPlayerIdAsync(Guid playerId)
        {
            return await _context.Transactions
                .Where(t => t.PlayerId == playerId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            return await _context.Transactions
                .Include(t => t.Player)
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetApprovedTransactionsByPlayerIdAsync(Guid playerId)
        {
            return await _context.Transactions
                .Where(t => t.PlayerId == playerId && t.IsApproved && !t.IsDeleted)
                .OrderByDescending(t => t.ApprovedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                transaction.IsDeleted = true;
                transaction.DeletedAt = DateTime.UtcNow;
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();
            }
        }
    }
}
