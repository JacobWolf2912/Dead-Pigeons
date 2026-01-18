using System.ComponentModel.DataAnnotations;
using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dead_Pigeons.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication for all endpoints in this controller
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerRepository _repository;

        public PlayersController(IPlayerRepository repository)
        {
            _repository = repository;
        }

        // POST: api/players
        // Only admin can create players
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePlayerRequest request)
        {
            // Validate input
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var player = new Player
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                IsActive = false, // Players start as inactive
                CreatedAt = DateTime.UtcNow
            };

            var result = await _repository.AddAsync(player);
            return Ok(result);
        }

        // GET: api/players
        // Only admin can see all players
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _repository.GetAllAsync());
        }

        // GET: api/players/{id}
        // Get a specific player (admin can see any, players can only see themselves)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var player = await _repository.GetByIdAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return Ok(player);
        }

        // POST: api/players/create-from-user/{userId}
        // Temporary endpoint: Create a Player record for an existing ApplicationUser (for testing)
        [HttpPost("create-from-user/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> CreatePlayerFromUser(Guid userId, [FromBody] CreatePlayerFromUserRequest request)
        {
            var player = new Player
            {
                Id = userId,
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _repository.AddAsync(player);
            return Ok(new
            {
                message = "Player created successfully",
                player = new
                {
                    id = result.Id,
                    fullName = result.FullName,
                    email = result.Email,
                    phoneNumber = result.PhoneNumber,
                    isActive = result.IsActive
                }
            });
        }

        // PUT: api/players/{id}
        // Only admin can update players
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlayerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var player = await _repository.GetByIdAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            // Update only the fields provided
            if (!string.IsNullOrEmpty(request.FullName))
                player.FullName = request.FullName;
            if (!string.IsNullOrEmpty(request.Email))
                player.Email = request.Email;
            if (!string.IsNullOrEmpty(request.PhoneNumber))
                player.PhoneNumber = request.PhoneNumber;
            if (request.IsActive.HasValue)
                player.IsActive = request.IsActive.Value;

            await _repository.UpdateAsync(player);
            return Ok(player);
        }
    }

    // Request models for API input
    public class CreatePlayerRequest
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 100 characters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; } = null!;
    }

    public class UpdatePlayerRequest
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 100 characters")]
        public string? FullName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        public bool? IsActive { get; set; }
    }

    public class CreatePlayerFromUserRequest
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 100 characters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; } = null!;
    }
}
