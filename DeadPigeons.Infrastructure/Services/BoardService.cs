using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;

namespace DeadPigeons.Infrastructure.Services
{
    public class BoardService : IBoardService
    {
        private readonly IBoardRepository _boardRepository;
        private readonly ITransactionService _transactionService;

        // Pricing table for boards based on field count
        private readonly Dictionary<int, decimal> _boardPrices = new()
        {
            { 5, 20 },      // 5 fields = 20 DKK
            { 6, 40 },      // 6 fields = 40 DKK
            { 7, 80 },      // 7 fields = 80 DKK
            { 8, 160 }      // 8 fields = 160 DKK
        };

        public BoardService(IBoardRepository boardRepository, ITransactionService transactionService)
        {
            _boardRepository = boardRepository;
            _transactionService = transactionService;
        }

        // Get the price for a board based on how many fields are selected
        public decimal GetBoardPrice(int fieldCount)
        {
            if (_boardPrices.TryGetValue(fieldCount, out var price))
            {
                return price;
            }

            throw new ArgumentException($"Invalid field count: {fieldCount}. Must be between 5 and 8.");
        }

        // Purchase a board - verifies player has enough balance and deducts the cost
        public async Task<Board> PurchaseBoardAsync(Guid playerId, Guid gameId, int fieldCount, List<int> numbers)
        {
            // Validate field count
            if (fieldCount < 5 || fieldCount > 8)
            {
                throw new ArgumentException("Field count must be between 5 and 8");
            }

            // Validate number of selected numbers matches field count
            if (numbers.Count != fieldCount)
            {
                throw new ArgumentException($"Expected {fieldCount} numbers, but got {numbers.Count}");
            }

            // Validate all numbers are unique (no duplicates)
            var uniqueNumbers = numbers.Distinct().ToList();
            if (uniqueNumbers.Count != numbers.Count)
            {
                throw new ArgumentException("All numbers must be unique - no duplicates allowed");
            }

            // Validate all numbers are between 1-16
            if (numbers.Any(n => n < 1 || n > 16))
            {
                throw new ArgumentException("All numbers must be between 1 and 16");
            }

            // Get the price for this board
            var price = GetBoardPrice(fieldCount);

            // Check player's balance
            var balance = await _transactionService.GetPlayerBalanceAsync(playerId);
            if (balance < price)
            {
                throw new InvalidOperationException($"Insufficient balance. Required: {price} DKK, Available: {balance} DKK");
            }

            // Create the board
            var board = new Board
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                GameId = gameId,
                FieldCount = fieldCount,
                Price = price,
                IsWinningBoard = false,
                CreatedAt = DateTime.UtcNow,
                Numbers = new List<BoardNumber>()
            };

            // Add the selected numbers to the board
            foreach (var number in numbers.OrderBy(n => n))
            {
                board.Numbers.Add(new BoardNumber
                {
                    Id = Guid.NewGuid(),
                    BoardId = board.Id,
                    Number = number
                });
            }

            // Save the board
            await _boardRepository.AddAsync(board);

            return board;
        }

        // Get all boards for a player
        public async Task<IEnumerable<Board>> GetPlayerBoardsAsync(Guid playerId)
        {
            return await _boardRepository.GetByPlayerIdAsync(playerId);
        }

        // Get all boards for a game
        public async Task<IEnumerable<Board>> GetGameBoardsAsync(Guid gameId)
        {
            return await _boardRepository.GetByGameIdAsync(gameId);
        }

        // Get a specific board
        public async Task<Board?> GetBoardAsync(Guid boardId)
        {
            return await _boardRepository.GetByIdAsync(boardId);
        }

        // Check if a board is a winning board
        // A board wins if it contains ALL the winning numbers (order doesn't matter)
        public bool IsWinningBoard(Board board, GameWinningNumbers winningNumbers)
        {
            // Get the numbers on this board
            var boardNumbers = board.Numbers.Select(bn => bn.Number).ToList();

            // Get the winning numbers
            var winningNumbersList = new List<int>
            {
                winningNumbers.Number1,
                winningNumbers.Number2,
                winningNumbers.Number3
            };

            // Check if board contains ALL winning numbers
            // Example: Board has [1, 2, 4, 5, 7]
            // Winning numbers: [2, 5, 1]
            // Result: True (board contains 1, 2, and 5)
            return winningNumbersList.All(wn => boardNumbers.Contains(wn));
        }
    }
}
