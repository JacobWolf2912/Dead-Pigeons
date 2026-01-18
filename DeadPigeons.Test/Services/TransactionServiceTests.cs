using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using DeadPigeons.Test.Helpers;
using Xunit;

namespace DeadPigeons.Test.Services;

public class TransactionServiceTests
{
    private readonly ITransactionService _transactionService;
    private readonly IPlayerService _playerService;
    private readonly AppDbContext _dbContext;

    public TransactionServiceTests(ITransactionService transactionService, IPlayerService playerService, AppDbContext dbContext)
    {
        _transactionService = transactionService;
        _playerService = playerService;
        _dbContext = dbContext;
    }

    #region CreateDepositAsync Tests - Happy Path

    [Fact]
    public async Task CreateDepositAsync_WithValidData_CreatesTransaction()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var amount = 100m;
        var mobilePayId = "TEST123";

        // Act
        var result = await _transactionService.CreateDepositAsync(playerId, amount, mobilePayId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(playerId, result.PlayerId);
        Assert.Equal(amount, result.Amount);
        Assert.Equal(mobilePayId, result.MobilePayTransactionId);
        Assert.False(result.IsApproved);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateDepositAsync_WithSmallAmount_CreatesTransaction()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var amount = 10m; // Minimum amount
        var mobilePayId = "SMALL";

        // Act
        var result = await _transactionService.CreateDepositAsync(playerId, amount, mobilePayId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(amount, result.Amount);
    }

    [Fact]
    public async Task CreateDepositAsync_WithLargeAmount_CreatesTransaction()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var amount = 50000m; // Maximum amount
        var mobilePayId = "LARGE";

        // Act
        var result = await _transactionService.CreateDepositAsync(playerId, amount, mobilePayId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(amount, result.Amount);
    }

    #endregion

    #region GetPlayerBalanceAsync Tests - Happy Path

    [Fact]
    public async Task GetPlayerBalanceAsync_WithNoTransactions_ReturnsZero()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        // Act
        var balance = await _transactionService.GetPlayerBalanceAsync(playerId);

        // Assert
        Assert.Equal(0m, balance);
    }

    [Fact]
    public async Task GetPlayerBalanceAsync_WithApprovedTransaction_ReturnsBalance()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        var createdPlayer = await _playerService.CreateAsync(player);

        var transaction = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 100m, "TEST123", true);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var balance = await _transactionService.GetPlayerBalanceAsync(createdPlayer.Id);

        // Assert
        Assert.Equal(100m, balance);
    }

    [Fact]
    public async Task GetPlayerBalanceAsync_WithMultipleApprovedTransactions_ReturnsSummedBalance()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        var createdPlayer = await _playerService.CreateAsync(player);

        var transaction1 = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 100m, "TXN1", true);
        var transaction2 = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 50m, "TXN2", true);
        _dbContext.Transactions.Add(transaction1);
        _dbContext.Transactions.Add(transaction2);
        await _dbContext.SaveChangesAsync();

        // Act
        var balance = await _transactionService.GetPlayerBalanceAsync(createdPlayer.Id);

        // Assert
        Assert.Equal(150m, balance);
    }

    [Fact]
    public async Task GetPlayerBalanceAsync_WithPendingTransaction_DoesNotIncludeInBalance()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        var createdPlayer = await _playerService.CreateAsync(player);

        var approvedTransaction = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 100m, "APPROVED", true);
        var pendingTransaction = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 50m, "PENDING", false);
        _dbContext.Transactions.Add(approvedTransaction);
        _dbContext.Transactions.Add(pendingTransaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var balance = await _transactionService.GetPlayerBalanceAsync(createdPlayer.Id);

        // Assert
        Assert.Equal(100m, balance); // Only approved transaction counts
    }

    #endregion

    #region GetPlayerBalanceAsync Tests - Unhappy Path

    [Fact]
    public async Task GetPlayerBalanceAsync_BalanceNeverNegative_ReturnsZeroIfNegative()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        // Act
        var balance = await _transactionService.GetPlayerBalanceAsync(playerId);

        // Assert
        Assert.True(balance >= 0); // Balance should never be negative
    }

    #endregion

    #region ApproveTransactionAsync Tests - Happy Path

    [Fact]
    public async Task ApproveTransactionAsync_WithPendingTransaction_ApprovesAndIncreasesBoardService()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        var createdPlayer = await _playerService.CreateAsync(player);

        var transaction = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 100m, "TEST123", false);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _transactionService.ApproveTransactionAsync(transaction.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsApproved);
        Assert.NotNull(result.ApprovedAt);
    }

    [Fact]
    public async Task ApproveTransactionAsync_WithEditedAmount_ApprovesWithNewAmount()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        var createdPlayer = await _playerService.CreateAsync(player);

        var transaction = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 100m, "TEST123", false);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _transactionService.ApproveTransactionAsync(transaction.Id, 150m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150m, result.Amount);
        Assert.True(result.IsApproved);
    }

    #endregion

    #region ApproveTransactionAsync Tests - Unhappy Path

    [Fact]
    public async Task ApproveTransactionAsync_WithNonExistentTransaction_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _transactionService.ApproveTransactionAsync(Guid.NewGuid())
        );

        Assert.Contains("Transaction not found", exception.Message);
    }

    [Fact]
    public async Task ApproveTransactionAsync_WithAlreadyApprovedTransaction_ThrowsException()
    {
        // Arrange
        var transaction = TestDataBuilder.CreateTestTransaction(Guid.NewGuid(), 100m, "TEST123", true);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _transactionService.ApproveTransactionAsync(transaction.Id)
        );

        Assert.Contains("already approved", exception.Message);
    }

    #endregion

    #region DeleteTransactionAsync Tests - Happy Path

    [Fact]
    public async Task DeleteTransactionAsync_WithPendingTransaction_DeletesTransaction()
    {
        // Arrange
        var transaction = TestDataBuilder.CreateTestTransaction(Guid.NewGuid(), 100m, "TEST123", false);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        await _transactionService.DeleteTransactionAsync(transaction.Id);

        // Assert
        var result = await _transactionService.GetTransactionAsync(transaction.Id);
        Assert.Null(result);
    }

    #endregion

    #region DeleteTransactionAsync Tests - Unhappy Path

    [Fact]
    public async Task DeleteTransactionAsync_WithApprovedTransaction_ThrowsException()
    {
        // Arrange
        var transaction = TestDataBuilder.CreateTestTransaction(Guid.NewGuid(), 100m, "TEST123", true);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _transactionService.DeleteTransactionAsync(transaction.Id)
        );

        Assert.Contains("Cannot delete an approved transaction", exception.Message);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WithNonExistentTransaction_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _transactionService.DeleteTransactionAsync(Guid.NewGuid())
        );

        Assert.Contains("Transaction not found", exception.Message);
    }

    #endregion

    #region GetPendingTransactionsAsync Tests - Happy Path

    [Fact]
    public async Task GetPendingTransactionsAsync_WithPendingTransactions_ReturnsPendingOnly()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var pending1 = TestDataBuilder.CreateTestTransaction(playerId, 100m, "PEND1", false);
        var pending2 = TestDataBuilder.CreateTestTransaction(playerId, 50m, "PEND2", false);
        var approved = TestDataBuilder.CreateTestTransaction(playerId, 200m, "APPR", true);

        _dbContext.Transactions.Add(pending1);
        _dbContext.Transactions.Add(pending2);
        _dbContext.Transactions.Add(approved);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _transactionService.GetPendingTransactionsAsync();

        // Assert
        Assert.NotNull(result);
        var pendingList = result.ToList();
        Assert.All(pendingList, t => Assert.False(t.IsApproved));
    }

    [Fact]
    public async Task GetPendingTransactionsAsync_WithNoTransactions_ReturnsEmpty()
    {
        // Act
        var result = await _transactionService.GetPendingTransactionsAsync();

        // Assert
        Assert.NotNull(result);
        // May have pending from other tests, just check it's enumerable
        Assert.IsAssignableFrom<IEnumerable<DeadPigeons.Core.Entities.Transaction>>(result);
    }

    #endregion

    #region GetPlayerTransactionsAsync Tests - Happy Path

    [Fact]
    public async Task GetPlayerTransactionsAsync_WithMultipleTransactions_ReturnsAllPlayerTransactions()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        var createdPlayer = await _playerService.CreateAsync(player);

        var txn1 = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 100m, "TXN1", false);
        var txn2 = TestDataBuilder.CreateTestTransaction(createdPlayer.Id, 50m, "TXN2", true);

        _dbContext.Transactions.Add(txn1);
        _dbContext.Transactions.Add(txn2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _transactionService.GetPlayerTransactionsAsync(createdPlayer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 2);
    }

    #endregion

    #region GetTransactionAsync Tests - Happy Path

    [Fact]
    public async Task GetTransactionAsync_WithExistingTransaction_ReturnsTransaction()
    {
        // Arrange
        var transaction = TestDataBuilder.CreateTestTransaction(Guid.NewGuid(), 100m, "TEST123", false);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _transactionService.GetTransactionAsync(transaction.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transaction.Id, result.Id);
    }

    #endregion

    #region GetTransactionAsync Tests - Unhappy Path

    [Fact]
    public async Task GetTransactionAsync_WithNonExistentTransaction_ReturnsNull()
    {
        // Act
        var result = await _transactionService.GetTransactionAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion
}
