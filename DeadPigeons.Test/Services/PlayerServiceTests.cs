using DeadPigeons.Core.Interfaces;
using DeadPigeons.Test.Helpers;
using Xunit;

namespace DeadPigeons.Test.Services;

public class PlayerServiceTests
{
    private readonly IPlayerService _playerService;

    public PlayerServiceTests(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    #region CreateAsync Tests - Happy Path

    [Fact]
    public async Task CreateAsync_WithValidPlayer_CreatesAndReturnsPlayer()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer("John Doe", "john@example.com");

        // Act
        var result = await _playerService.CreateAsync(player);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(player.FullName, result.FullName);
        Assert.Equal(player.Email, result.Email);
        Assert.Equal(player.PhoneNumber, result.PhoneNumber);
        Assert.False(result.IsActive); // Should be inactive by default
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_WithMultiplePlayers_CreatesAllPlayers()
    {
        // Arrange
        var player1 = TestDataBuilder.CreateTestPlayer("Player 1", "player1@test.com");
        var player2 = TestDataBuilder.CreateTestPlayer("Player 2", "player2@test.com");

        // Act
        var result1 = await _playerService.CreateAsync(player1);
        var result2 = await _playerService.CreateAsync(player2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1.Id, result2.Id);
    }

    #endregion

    #region GetByIdAsync Tests - Happy Path

    [Fact]
    public async Task GetByIdAsync_WithExistingPlayer_ReturnsPlayer()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        var createdPlayer = await _playerService.CreateAsync(player);

        // Act
        var result = await _playerService.GetByIdAsync(createdPlayer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdPlayer.Id, result.Id);
        Assert.Equal(createdPlayer.FullName, result.FullName);
    }

    #endregion

    #region GetByIdAsync Tests - Unhappy Path

    [Fact]
    public async Task GetByIdAsync_WithNonExistentPlayerId_ReturnsNull()
    {
        // Act
        var result = await _playerService.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests - Happy Path

    [Fact]
    public async Task GetAllAsync_WithMultiplePlayers_ReturnsAllPlayers()
    {
        // Arrange
        var player1 = TestDataBuilder.CreateTestPlayer("Player 1", "player1@test.com");
        var player2 = TestDataBuilder.CreateTestPlayer("Player 2", "player2@test.com");
        var player3 = TestDataBuilder.CreateTestPlayer("Player 3", "player3@test.com");

        await _playerService.CreateAsync(player1);
        await _playerService.CreateAsync(player2);
        await _playerService.CreateAsync(player3);

        // Act
        var result = await _playerService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    [Fact]
    public async Task GetAllAsync_WithNoPlayers_ReturnsEmptyList()
    {
        // Act
        var result = await _playerService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region UpdateAsync Tests - Happy Path

    [Fact]
    public async Task UpdateAsync_WithValidChanges_UpdatesPlayer()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer("Original Name");
        var createdPlayer = await _playerService.CreateAsync(player);
        createdPlayer.FullName = "Updated Name";

        // Act
        await _playerService.UpdateAsync(createdPlayer);
        var result = await _playerService.GetByIdAsync(createdPlayer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.FullName);
    }

    [Fact]
    public async Task UpdateAsync_ChangingPhoneNumber_UpdatesSuccessfully()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer("Test", "test@test.com", "1111111111");
        var createdPlayer = await _playerService.CreateAsync(player);
        createdPlayer.PhoneNumber = "9999999999";

        // Act
        await _playerService.UpdateAsync(createdPlayer);
        var result = await _playerService.GetByIdAsync(createdPlayer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("9999999999", result.PhoneNumber);
    }

    #endregion

    #region SetActiveStatusAsync Tests - Happy Path

    [Fact]
    public async Task SetActiveStatusAsync_ActivatingInactivePlayer_MarksPlayerActive()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        var createdPlayer = await _playerService.CreateAsync(player);
        Assert.False(createdPlayer.IsActive); // Verify initially inactive

        // Act
        await _playerService.SetActiveStatusAsync(createdPlayer.Id, true);
        var result = await _playerService.GetByIdAsync(createdPlayer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task SetActiveStatusAsync_DeactivatingActivePlayer_MarksPlayerInactive()
    {
        // Arrange
        var player = TestDataBuilder.CreateTestPlayer();
        player.IsActive = true;
        var createdPlayer = await _playerService.CreateAsync(player);

        // Act
        await _playerService.SetActiveStatusAsync(createdPlayer.Id, false);
        var result = await _playerService.GetByIdAsync(createdPlayer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    #endregion

    #region SetActiveStatusAsync Tests - Unhappy Path

    [Fact]
    public async Task SetActiveStatusAsync_WithNonExistentPlayer_ThrowsException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _playerService.SetActiveStatusAsync(Guid.NewGuid(), true)
        );

        Assert.Contains("Player not found", exception.Message);
    }

    #endregion
}
