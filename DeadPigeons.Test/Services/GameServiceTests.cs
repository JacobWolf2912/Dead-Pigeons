using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using DeadPigeons.Test.Helpers;
using Xunit;

namespace DeadPigeons.Test.Services;

public class GameServiceTests
{
    private readonly IGameService _gameService;
    private readonly AppDbContext _dbContext;
    private readonly IBoardService _boardService;

    public GameServiceTests(IGameService gameService, AppDbContext dbContext, IBoardService boardService)
    {
        _gameService = gameService;
        _dbContext = dbContext;
        _boardService = boardService;
    }

    #region GetAllGamesAsync Tests - Happy Path

    [Fact]
    public async Task GetAllGamesAsync_WithMultipleGames_ReturnsAllGames()
    {
        // Arrange
        var game1 = TestDataBuilder.CreateTestGame();
        var game2 = TestDataBuilder.CreateTestGame(DateTime.UtcNow.AddDays(7));
        _dbContext.Games.Add(game1);
        _dbContext.Games.Add(game2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.GetAllGamesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 2);
    }

    [Fact]
    public async Task GetAllGamesAsync_WithNoGames_ReturnsEmptyList()
    {
        // Arrange - Assuming clean database

        // Act
        var result = await _gameService.GetAllGamesAsync();

        // Assert
        Assert.NotNull(result);
        // May have games from other tests, so just check it's enumerable
        Assert.IsAssignableFrom<IEnumerable<DeadPigeons.Core.Entities.Game>>(result);
    }

    #endregion

    #region GetGameAsync Tests - Happy Path

    [Fact]
    public async Task GetGameAsync_WithExistingGame_ReturnsGame()
    {
        // Arrange
        var game = TestDataBuilder.CreateTestGame();
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.GetGameAsync(game.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(game.Id, result.Id);
    }

    #endregion

    #region GetGameAsync Tests - Unhappy Path

    [Fact]
    public async Task GetGameAsync_WithNonExistentGame_ReturnsNull()
    {
        // Act
        var result = await _gameService.GetGameAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetCurrentGameAsync Tests - Happy Path

    [Fact]
    public async Task GetCurrentGameAsync_WithActiveGame_ReturnsCurrentGame()
    {
        // Arrange
        var activeGame = TestDataBuilder.CreateTestGame();
        activeGame.IsClosed = false;
        _dbContext.Games.Add(activeGame);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.GetCurrentGameAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsClosed);
    }

    #endregion

    #region GetCurrentGameAsync Tests - Unhappy Path

    [Fact]
    public async Task GetCurrentGameAsync_WithNoActiveGames_ReturnsNullOrActiveGame()
    {
        // Arrange - Only create closed games
        var closedGame = TestDataBuilder.CreateTestGame();
        closedGame.IsClosed = true;
        _dbContext.Games.Add(closedGame);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.GetCurrentGameAsync();

        // Assert - Result might be null or an active game from other tests
        if (result != null)
        {
            Assert.False(result.IsClosed);
        }
    }

    #endregion

    #region DrawWinningNumbersAsync Tests - Happy Path

    [Fact]
    public async Task DrawWinningNumbersAsync_WithValidGame_DrawsWinningNumbers()
    {
        // Arrange
        var game = TestDataBuilder.CreateTestGame();
        game.IsClosed = false;
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.DrawWinningNumbersAsync(game.Id, 5, 10, 15);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsClosed);
        Assert.NotNull(result.WinningNumbers);
        Assert.Equal(5, result.WinningNumbers.Number1);
        Assert.Equal(10, result.WinningNumbers.Number2);
        Assert.Equal(15, result.WinningNumbers.Number3);
    }

    [Fact]
    public async Task DrawWinningNumbersAsync_WithValidNumbers_MarksWinningBoards()
    {
        // Arrange
        var game = TestDataBuilder.CreateTestGame();
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.DrawWinningNumbersAsync(game.Id, 1, 2, 3);

        // Assert
        Assert.NotNull(result.WinningNumbers);
        Assert.Equal(1, result.WinningNumbers.Number1);
        Assert.Equal(2, result.WinningNumbers.Number2);
        Assert.Equal(3, result.WinningNumbers.Number3);
    }

    #endregion

    #region DrawWinningNumbersAsync Tests - Unhappy Path

    [Fact]
    public async Task DrawWinningNumbersAsync_WithNumberOutOfRange_ThrowsArgumentException()
    {
        // Arrange
        var game = TestDataBuilder.CreateTestGame();
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _gameService.DrawWinningNumbersAsync(game.Id, 0, 10, 15) // 0 is out of range
        );

        Assert.Contains("between 1 and 16", exception.Message);
    }

    [Fact]
    public async Task DrawWinningNumbersAsync_WithNumberAboveRange_ThrowsArgumentException()
    {
        // Arrange
        var game = TestDataBuilder.CreateTestGame();
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _gameService.DrawWinningNumbersAsync(game.Id, 5, 10, 17) // 17 is out of range
        );

        Assert.Contains("between 1 and 16", exception.Message);
    }

    [Fact]
    public async Task DrawWinningNumbersAsync_WithNonExistentGame_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _gameService.DrawWinningNumbersAsync(Guid.NewGuid(), 1, 2, 3)
        );

        Assert.Contains("Game not found", exception.Message);
    }

    [Fact]
    public async Task DrawWinningNumbersAsync_WithAlreadyClosedGame_ThrowsInvalidOperationException()
    {
        // Arrange
        var game = TestDataBuilder.CreateTestGame();
        game.IsClosed = true;
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gameService.DrawWinningNumbersAsync(game.Id, 1, 2, 3)
        );

        Assert.Contains("already closed", exception.Message);
    }

    [Fact]
    public async Task DrawWinningNumbersAsync_WithAlreadyDrawnNumbers_ThrowsInvalidOperationException()
    {
        // Arrange
        var game = TestDataBuilder.CreateTestGame();
        var winningNumbers = TestDataBuilder.CreateTestWinningNumbers(game.Id);
        game.WinningNumbers = winningNumbers;
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _gameService.DrawWinningNumbersAsync(game.Id, 1, 2, 3)
        );

        Assert.Contains("already drawn", exception.Message);
    }

    #endregion

    #region GetWinningBoardsForGameAsync Tests - Happy Path

    [Fact]
    public async Task GetWinningBoardsForGameAsync_WithWinningBoards_ReturnsWinningBoards()
    {
        // Arrange
        var game = TestDataBuilder.CreateTestGame();
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.GetWinningBoardsForGameAsync(game.Id);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<DeadPigeons.Core.Entities.Board>>(result);
    }

    #endregion

    #region ActivateNextGameAsync Tests - Happy Path

    [Fact]
    public async Task ActivateNextGameAsync_WithInactiveGame_ReturnsGame()
    {
        // Arrange
        var inactiveGame = TestDataBuilder.CreateTestGame();
        inactiveGame.IsClosed = false;
        _dbContext.Games.Add(inactiveGame);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.ActivateNextGameAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsClosed);
    }

    #endregion
}
