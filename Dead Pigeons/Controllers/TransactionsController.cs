using System.ComponentModel.DataAnnotations;
using DeadPigeons.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dead_Pigeons.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        // POST: api/transactions/deposit
        // Players deposit money towards their balance
        [HttpPost("deposit")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
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

            // Create the deposit transaction (starts as pending)
            var transaction = await _transactionService.CreateDepositAsync(playerId, request.Amount, request.MobilePayTransactionId);

            return Ok(new
            {
                message = "Deposit submitted. Awaiting admin approval.",
                transaction = new
                {
                    id = transaction.Id,
                    amount = transaction.Amount,
                    mobilePayId = transaction.MobilePayTransactionId,
                    status = "Pending",
                    createdAt = transaction.CreatedAt
                }
            });
        }

        // GET: api/transactions/balance?playerId=xxx
        // Get player's current balance
        [HttpGet("balance")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBalance([FromQuery] Guid? playerId = null)
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

            var balance = await _transactionService.GetPlayerBalanceAsync(userId);

            return Ok(new
            {
                balance = balance,
                currency = "DKK"
            });
        }

        // GET: api/transactions/my-transactions?playerId=xxx
        // Get player's own transaction history
        [HttpGet("my-transactions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMyTransactions([FromQuery] Guid? playerId = null)
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

            var transactions = await _transactionService.GetPlayerTransactionsAsync(userId);

            return Ok(transactions.Select(t => new
            {
                id = t.Id,
                amount = t.Amount,
                mobilePayId = t.MobilePayTransactionId,
                status = t.IsApproved ? "Approved" : "Pending",
                createdAt = t.CreatedAt,
                approvedAt = t.ApprovedAt
            }));
        }

        // GET: api/transactions/pending
        // Only admin can see all pending transactions
        [HttpGet("pending")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> GetPendingTransactions()
        {
            var transactions = await _transactionService.GetPendingTransactionsAsync();

            return Ok(transactions.Select(t => new
            {
                id = t.Id,
                playerId = t.PlayerId,
                playerName = t.Player?.FullName,
                playerEmail = t.Player?.Email,
                amount = t.Amount,
                mobilePayId = t.MobilePayTransactionId,
                createdAt = t.CreatedAt
            }));
        }

        // POST: api/transactions/{id}/approve
        // Only admin can approve pending transactions (optionally with edited amount)
        [HttpPost("{id}/approve")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> ApproveTransaction(Guid id, [FromBody] ApproveTransactionRequest? request = null)
        {
            var transaction = await _transactionService.GetTransactionAsync(id);
            if (transaction == null)
            {
                return NotFound(new { error = "Transaction not found" });
            }

            if (transaction.IsApproved)
            {
                return BadRequest(new { error = "Transaction already approved" });
            }

            // Use provided amount or original amount
            decimal approveAmount = request?.Amount ?? transaction.Amount;

            if (approveAmount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than 0" });
            }

            var approvedTransaction = await _transactionService.ApproveTransactionAsync(id, approveAmount);

            return Ok(new
            {
                message = "Transaction approved successfully",
                transaction = new
                {
                    id = approvedTransaction.Id,
                    amount = approvedTransaction.Amount,
                    status = "Approved",
                    approvedAt = approvedTransaction.ApprovedAt
                }
            });
        }

        // POST: api/transactions/{id}/dismiss
        // Only admin can dismiss pending transactions
        [HttpPost("{id}/dismiss")]
        [AllowAnonymous] // Temporary: for testing purposes
        public async Task<IActionResult> DismissTransaction(Guid id)
        {
            var transaction = await _transactionService.GetTransactionAsync(id);
            if (transaction == null)
            {
                return NotFound(new { error = "Transaction not found" });
            }

            if (transaction.IsApproved)
            {
                return BadRequest(new { error = "Cannot dismiss an already approved transaction" });
            }

            // Delete the transaction
            await _transactionService.DeleteTransactionAsync(id);

            return Ok(new
            {
                message = "Transaction dismissed successfully",
                transactionId = id
            });
        }
    }

    // Request models for API input
    public class ApproveTransactionRequest
    {
        [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000 DKK")]
        public decimal Amount { get; set; }
    }

    public class DepositRequest
    {
        public Guid PlayerId { get; set; } = Guid.Empty; // Optional: for testing. If empty, uses authenticated user

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000 DKK")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "MobilePay transaction ID is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "MobilePay ID must be between 1 and 50 characters")]
        public string MobilePayTransactionId { get; set; } = null!;
    }
}
