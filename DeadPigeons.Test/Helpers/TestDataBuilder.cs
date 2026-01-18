using DeadPigeons.Core.Entities;

namespace DeadPigeons.Test.Helpers;

public static class TestDataBuilder
{
    public static Player CreateTestPlayer(string fullName = "Test Player", string email = "test@test.com", string phoneNumber = "1234567890")
    {
        return new Player
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Game CreateTestGame(DateTime? weekStart = null)
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            WeekStart = weekStart ?? DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek) + TimeSpan.FromDays(7),
            DrawTime = DateTime.UtcNow.AddDays(5),
            IsClosed = false
        };
    }

    public static Board CreateTestBoard(Guid playerId, Guid gameId, List<int>? numbers = null, int fieldCount = 5)
    {
        numbers ??= new List<int> { 1, 2, 3, 4, 5 };

        var board = new Board
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            GameId = gameId,
            FieldCount = fieldCount,
            Price = GetBoardPrice(fieldCount),
            IsWinningBoard = false,
            CreatedAt = DateTime.UtcNow,
            Numbers = new List<BoardNumber>()
        };

        foreach (var num in numbers)
        {
            board.Numbers.Add(new BoardNumber
            {
                Id = Guid.NewGuid(),
                BoardId = board.Id,
                Number = num
            });
        }

        return board;
    }

    public static GameWinningNumbers CreateTestWinningNumbers(Guid gameId, int number1 = 1, int number2 = 2, int number3 = 3)
    {
        return new GameWinningNumbers
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Number1 = number1,
            Number2 = number2,
            Number3 = number3,
            DrawnAt = DateTime.UtcNow
        };
    }

    public static Transaction CreateTestTransaction(Guid playerId, decimal amount = 100, string mobilePayId = "TEST123", bool isApproved = false)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Amount = amount,
            MobilePayTransactionId = mobilePayId,
            IsApproved = isApproved,
            ApprovedAt = isApproved ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static decimal GetBoardPrice(int fieldCount)
    {
        return fieldCount switch
        {
            5 => 20m,
            6 => 40m,
            7 => 80m,
            8 => 160m,
            _ => throw new ArgumentException("Invalid field count")
        };
    }
}
