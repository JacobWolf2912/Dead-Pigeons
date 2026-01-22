using System.ComponentModel.DataAnnotations;
using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dead_Pigeons.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly IBoardService _boardService;
        private readonly AppDbContext _context;

        public GamesController(IGameService gameService, IBoardService boardService, AppDbContext context)
        {
            _gameService = gameService;
            _boardService = boardService;
            _context = context;
        }

        // GET: api/games
        // Get all games sorted by date, with status information
        [HttpGet]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> GetAllGames()
        {
            var games = await _gameService.GetAllGamesAsync();

            // Get the current active game for comparison
            var currentGame = await _gameService.GetCurrentGameAsync();

            // Sort games by week start date (most recent first)
            var sortedGames = games.OrderByDescending(g => g.WeekStart).ToList();

            return Ok(new
            {
                currentGameId = currentGame?.Id,
                games = sortedGames.Select(g => new
                {
                    id = g.Id,
                    weekStart = g.WeekStart,
                    drawTime = g.DrawTime,
                    isClosed = g.IsClosed,
                    status = g.IsClosed && g.WinningNumbers != null ? "Completed" :
                             g.IsClosed && g.WinningNumbers == null ? "Closed-PendingNumbers" :
                             !g.IsClosed && g.WinningNumbers == null ? "Open" : "Unknown",
                    isCurrentGame = g.Id == currentGame?.Id,
                    winningNumbers = g.WinningNumbers != null ? new
                    {
                        number1 = g.WinningNumbers.Number1,
                        number2 = g.WinningNumbers.Number2,
                        number3 = g.WinningNumbers.Number3,
                        drawnAt = g.WinningNumbers.DrawnAt
                    } : null,
                    boardCount = g.Boards.Count,
                    winningBoardCount = g.WinningNumbers != null ? g.Boards.Count(b => b.IsWinningBoard) : 0
                })
            });
        }

        // GET: api/games/{id}
        // Get a specific game
        [HttpGet("{id}")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> GetGame(Guid id)
        {
            var game = await _gameService.GetGameAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                id = game.Id,
                weekStart = game.WeekStart,
                drawTime = game.DrawTime,
                isClosed = game.IsClosed,
                winningNumbers = game.WinningNumbers != null ? new
                {
                    number1 = game.WinningNumbers.Number1,
                    number2 = game.WinningNumbers.Number2,
                    number3 = game.WinningNumbers.Number3,
                    drawnAt = game.WinningNumbers.DrawnAt
                } : null,
                boardCount = game.Boards.Count,
                winningBoardCount = game.Boards.Count(b => b.IsWinningBoard)
            });
        }

        // GET: api/games/current
        // Get the current active game with detailed information
        [HttpGet("current")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> GetCurrentGame()
        {
            var game = await _gameService.GetCurrentGameAsync();
            if (game == null)
            {
                return NotFound(new { error = "No active game found" });
            }

            // Calculate time remaining before game closes (Saturday 17:00 Copenhagen time)
            TimeZoneInfo copenhagenZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            DateTime copenhagenNow = TimeZoneInfo.ConvertTime(DateTime.Now, copenhagenZone);
            TimeSpan timeRemaining = game.DrawTime - copenhagenNow;

            return Ok(new
            {
                id = game.Id,
                weekStart = game.WeekStart,
                drawTime = game.DrawTime,
                isClosed = game.IsClosed,
                status = "Open",
                closesAt = $"Saturday 17:00 Copenhagen time",
                boardCount = game.Boards.Count,
                hoursRemaining = Math.Max(0, timeRemaining.TotalHours)
            });
        }

        // POST: api/games/{id}/draw-numbers
        // Admin draws winning numbers for a closed game (within 24 hours of closure)
        [HttpPost("{id}/draw-numbers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DrawWinningNumbers(Guid id, [FromBody] DrawNumbersRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var game = await _gameService.GetGameAsync(id);
                if (game == null)
                {
                    return NotFound(new { error = "Game not found" });
                }

                // Check if game is closed
                if (!game.IsClosed)
                {
                    return BadRequest(new { error = "Game must be closed before drawing numbers. Game closes at Saturday 17:00." });
                }

                // Check if numbers already drawn
                if (game.WinningNumbers != null)
                {
                    return BadRequest(new { error = "Winning numbers have already been drawn for this game" });
                }

                // Check if within 24 hours of closing
                var timeExpired = DateTime.UtcNow > game.DrawTime.AddHours(24);
                if (timeExpired)
                {
                    return BadRequest(new { error = "24-hour window to input numbers has expired. Please refund the game." });
                }

                var updatedGame = await _gameService.DrawWinningNumbersAsync(id, request.Number1, request.Number2, request.Number3);

                // Get winning boards
                var winningBoards = await _gameService.GetWinningBoardsForGameAsync(id);

                return Ok(new
                {
                    message = "Winning numbers drawn successfully",
                    game = new
                    {
                        id = updatedGame.Id,
                        weekStart = updatedGame.WeekStart,
                        drawTime = updatedGame.DrawTime,
                        isClosed = updatedGame.IsClosed,
                        winningNumbers = new
                        {
                            number1 = updatedGame.WinningNumbers!.Number1,
                            number2 = updatedGame.WinningNumbers.Number2,
                            number3 = updatedGame.WinningNumbers.Number3,
                            drawnAt = updatedGame.WinningNumbers.DrawnAt
                        },
                        totalBoards = updatedGame.Boards.Count,
                        winningBoards = winningBoards.Count()
                    }
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
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: api/games/{id}/refund
        // Admin refunds all boards if 24 hours have passed without drawing numbers
        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefundGame(Guid id)
        {
            try
            {
                var game = await _gameService.GetGameAsync(id);
                if (game == null)
                {
                    return NotFound(new { error = "Game not found" });
                }

                // Check if game is closed
                if (!game.IsClosed)
                {
                    return BadRequest(new { error = "Only closed games can be refunded" });
                }

                // Check if numbers have already been drawn
                if (game.WinningNumbers != null)
                {
                    return BadRequest(new { error = "Cannot refund a game that has already drawn winning numbers" });
                }

                // Check if 24 hours have passed since closing
                var timeExpired = DateTime.UtcNow <= game.DrawTime.AddHours(24);
                if (timeExpired)
                {
                    var hoursRemaining = (game.DrawTime.AddHours(24) - DateTime.UtcNow).TotalHours;
                    return BadRequest(new { error = $"Cannot refund yet. {hoursRemaining:F1} hours remaining in the 24-hour window." });
                }

                // Delete all boards for this game
                var boards = _context.Boards.Where(b => b.GameId == id).ToList();
                _context.Boards.RemoveRange(boards);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Game refunded successfully",
                    refundedBoardCount = boards.Count,
                    details = $"{boards.Count} boards have been cancelled and players will receive refunds"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/games/{id}/winning-boards
        // Get all winning boards for a game
        [HttpGet("{id}/winning-boards")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> GetWinningBoards(Guid id)
        {
            var game = await _gameService.GetGameAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            if (game.WinningNumbers == null)
            {
                return BadRequest(new { error = "Game has not been drawn yet" });
            }

            var winningBoards = await _gameService.GetWinningBoardsForGameAsync(id);

            return Ok(new
            {
                gameId = id,
                winningNumbers = new
                {
                    number1 = game.WinningNumbers.Number1,
                    number2 = game.WinningNumbers.Number2,
                    number3 = game.WinningNumbers.Number3
                },
                winningBoards = winningBoards.Select(b => new
                {
                    id = b.Id,
                    playerId = b.PlayerId,
                    fieldCount = b.FieldCount,
                    price = b.Price,
                    numbers = b.Numbers.Select(bn => bn.Number).OrderBy(n => n).ToList()
                })
            });
        }

        // POST: api/games/create-test-game
        // Temporary endpoint: Create a test game for development/testing
        [HttpPost("create-test-game")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTestGame()
        {
            var game = new Game
            {
                Id = Guid.NewGuid(),
                WeekStart = DateTime.UtcNow,
                DrawTime = DateTime.UtcNow.AddDays(7),
                IsClosed = false,
                WinningNumbers = null,
                Boards = new List<Board>()
            };

            // Save to database
            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Test game created",
                game = new
                {
                    id = game.Id,
                    weekStart = game.WeekStart,
                    drawTime = game.DrawTime,
                    isClosed = game.IsClosed
                }
            });
        }

        // POST: api/games/reopen-current
        // Temporary endpoint: Reopen the current game for testing
        [HttpPost("reopen-current")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReopenCurrentGame()
        {
            var currentGame = await _gameService.GetCurrentGameAsync();

            if (currentGame == null)
            {
                // If no current open game, find the most recent closed one and reopen it
                var lastGame = await _context.Games
                    .OrderByDescending(g => g.WeekStart)
                    .FirstOrDefaultAsync();

                if (lastGame == null)
                {
                    return BadRequest(new { error = "No games found in database" });
                }

                lastGame.IsClosed = false;
                lastGame.WinningNumbers = null;
                _context.Games.Update(lastGame);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Most recent game reopened",
                    game = new
                    {
                        id = lastGame.Id,
                        weekStart = lastGame.WeekStart,
                        drawTime = lastGame.DrawTime,
                        isClosed = lastGame.IsClosed,
                        boardCount = lastGame.Boards?.Count ?? 0
                    }
                });
            }

            return BadRequest(new { error = "Current game is already open" });
        }

        // POST: api/games/create-closed-test-game
        // Temporary endpoint: Create a closed test game ready for drawing winning numbers
        [HttpPost("create-closed-test-game")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateClosedTestGame()
        {
            var game = new Game
            {
                Id = Guid.NewGuid(),
                WeekStart = DateTime.UtcNow.AddDays(-1),
                DrawTime = DateTime.UtcNow.AddHours(-1), // Closed 1 hour ago
                IsClosed = true,  // Game is already closed, ready for number drawing
                WinningNumbers = null,
                Boards = new List<Board>()
            };

            // Save to database
            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Closed test game created (ready for drawing winning numbers)",
                game = new
                {
                    id = game.Id,
                    weekStart = game.WeekStart,
                    drawTime = game.DrawTime,
                    isClosed = game.IsClosed,
                    boardCount = 0,
                    hoursUntilExpiry = 24 - ((DateTime.UtcNow - game.DrawTime).TotalHours)
                }
            });
        }

        // GET: api/games/pricing
        // Get board pricing info (public)
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
                }
            });
        }
    }

    // Request models for API input
    public class DrawNumbersRequest
    {
        [Required(ErrorMessage = "Number 1 is required")]
        [Range(1, 16, ErrorMessage = "Number 1 must be between 1 and 16")]
        public int Number1 { get; set; }

        [Required(ErrorMessage = "Number 2 is required")]
        [Range(1, 16, ErrorMessage = "Number 2 must be between 1 and 16")]
        public int Number2 { get; set; }

        [Required(ErrorMessage = "Number 3 is required")]
        [Range(1, 16, ErrorMessage = "Number 3 must be between 1 and 16")]
        public int Number3 { get; set; }
    }
}
