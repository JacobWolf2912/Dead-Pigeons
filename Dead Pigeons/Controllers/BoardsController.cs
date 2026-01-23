using System.ComponentModel.DataAnnotations;
using DeadPigeons.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dead_Pigeons.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class BoardsController : ControllerBase
    {
        private readonly IBoardService _boardService;
        private readonly ITransactionService _transactionService;

        public BoardsController(IBoardService boardService, ITransactionService transactionService)
        {
            _boardService = boardService;
            _transactionService = transactionService;
        }

        // POST: api/boards/purchase
        // Player purchases a board for a specific game
        [HttpPost("purchase")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> PurchaseBoard([FromBody] PurchaseBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get player ID from request or from authenticated user
            Guid playerId;

            if (request.PlayerId != Guid.Empty)
            {
                // Use the player ID from request (for testing)
                playerId = request.PlayerId;
            }
            else
            {
                // Try to get from authenticated user
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out playerId))
                {
                    return BadRequest(new { error = "PlayerId is required in request body or you must be authenticated" });
                }
            }

            try
            {
                // Purchase the board
                var board = await _boardService.PurchaseBoardAsync(playerId, request.GameId, request.FieldCount, request.Numbers);

                // Get updated balance after purchase
                var newBalance = await _transactionService.GetPlayerBalanceAsync(playerId);

                return Ok(new
                {
                    message = "Board purchased successfully",
                    board = new
                    {
                        id = board.Id,
                        gameId = board.GameId,
                        fieldCount = board.FieldCount,
                        price = board.Price,
                        numbers = board.Numbers.Select(bn => bn.Number).OrderBy(n => n).ToList(),
                        createdAt = board.CreatedAt
                    },
                    newBalance = newBalance
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/boards/my-boards?playerId=xxx
        // Get all boards purchased by the current player
        [HttpGet("my-boards")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMyBoards([FromQuery] Guid? playerId = null)
        {
            Guid userId;

            if (playerId.HasValue && playerId != Guid.Empty)
            {
                userId = playerId.Value;
            }
            else
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out userId))
                {
                    return BadRequest(new { error = "playerId query parameter is required or you must be authenticated" });
                }
            }

            var boards = await _boardService.GetPlayerBoardsAsync(userId);

            return Ok(boards.Select(b => new
            {
                id = b.Id,
                gameId = b.GameId,
                weekStart = b.Game?.WeekStart,
                weekNumber = b.Game != null ? $"Week of {new DateTime(b.Game.WeekStart.Year, b.Game.WeekStart.Month, b.Game.WeekStart.Day):MMM dd, yyyy}" : "Unknown",
                fieldCount = b.FieldCount,
                price = b.Price,
                numbers = b.Numbers.Select(bn => bn.Number).OrderBy(n => n).ToList(),
                isWinning = b.IsWinningBoard,
                winningNumbers = b.Game?.WinningNumbers != null ? new
                {
                    number1 = b.Game.WinningNumbers.Number1,
                    number2 = b.Game.WinningNumbers.Number2,
                    number3 = b.Game.WinningNumbers.Number3
                } : null,
                createdAt = b.CreatedAt
            }));
        }

        // GET: api/boards/{id}
        // Get a specific board
        [HttpGet("{id}")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> GetBoard(Guid id)
        {
            var board = await _boardService.GetBoardAsync(id);
            if (board == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                id = board.Id,
                playerId = board.PlayerId,
                gameId = board.GameId,
                fieldCount = board.FieldCount,
                price = board.Price,
                numbers = board.Numbers.Select(bn => bn.Number).OrderBy(n => n).ToList(),
                isWinning = board.IsWinningBoard,
                createdAt = board.CreatedAt
            });
        }

        // GET: api/boards/game/{gameId}
        // Get all boards for a specific game (admin only)
        [HttpGet("game/{gameId}")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> GetGameBoards(Guid gameId)
        {
            var boards = await _boardService.GetGameBoardsAsync(gameId);

            return Ok(boards.Select(b => new
            {
                id = b.Id,
                playerId = b.PlayerId,
                fieldCount = b.FieldCount,
                price = b.Price,
                numbers = b.Numbers.Select(bn => bn.Number).OrderBy(n => n).ToList(),
                isWinning = b.IsWinningBoard,
                createdAt = b.CreatedAt
            }));
        }

        // GET: api/boards/pricing
        // Get board pricing information
        [HttpGet("pricing")]
        [AllowAnonymous]
        public IActionResult GetPricing()
        {
            return Ok(new
            {
                prices = new
                {
                    fields5 = "20 DKK",
                    fields6 = "40 DKK",
                    fields7 = "80 DKK",
                    fields8 = "160 DKK"
                },
                description = "Select between 5-8 numbers from 1-16"
            });
        }
    }

    // Request models for API input
    public class PurchaseBoardRequest
    {
        public Guid PlayerId { get; set; } = Guid.Empty; // Optional: for testing. If empty, uses authenticated user

        [Required(ErrorMessage = "Game ID is required")]
        public Guid GameId { get; set; }

        [Required(ErrorMessage = "Field count is required")]
        [Range(5, 8, ErrorMessage = "Field count must be between 5 and 8")]
        public int FieldCount { get; set; }

        [Required(ErrorMessage = "Numbers are required")]
        public List<int> Numbers { get; set; } = new();
    }
}
