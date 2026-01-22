using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using DeadPigeons.Core.Entities;
using DeadPigeons.Core.Interfaces;
using DeadPigeons.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dead_Pigeons.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IPlayerService _playerService;
        private readonly AppDbContext _dbContext;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IPlayerService playerService,
            AppDbContext dbContext,
            JwtSettings jwtSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _playerService = playerService;
            _dbContext = dbContext;
            _jwtSettings = jwtSettings;
        }

        // Generate JWT token for authenticated user
        private string GenerateJwtToken(ApplicationUser user, bool isAdmin)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email ?? ""),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.FullName ?? ""),
            };

            if (isAdmin)
            {
                claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"));
            }
            else
            {
                claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Player"));
            }

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // POST: api/auth/register
        // Creates a pending player registration waiting for admin approval
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Basic validation
            if (!ModelState.IsValid)
            {
                var errorMessages = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(new { error = errorMessages });
            }

            // Validate full name format (must include first and last name)
            var fullNamePattern = new Regex(@"^[a-zA-Z]{2,}\s+[a-zA-Z]{2,}$");
            if (!fullNamePattern.IsMatch(request.FullName))
            {
                return BadRequest(new { error = "Full name must include first and last name (minimum 2 characters each)" });
            }

            // Validate phone number format (Danish)
            var phonePattern = new Regex(@"^(\d{8}|\d{10}|(\+45)?[-\s]?\d{4}[-\s]?\d{4})$");
            if (!phonePattern.IsMatch(request.PhoneNumber))
            {
                return BadRequest(new { error = "Phone number must be 8 digits" });
            }

            // Validate password strength (must include special character)
            var passwordPattern = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*\-_=+,.]).{6,}$");
            if (!passwordPattern.IsMatch(request.Password))
            {
                return BadRequest(new { error = "Password must contain uppercase, lowercase, number, and special character (!@#$%^&*-_=+,.)" });
            }

            // Check if email already exists in users OR pending players
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "Email already registered" });
            }

            var existingPending = _dbContext.PendingPlayers.FirstOrDefault(p => p.Email == request.Email);
            if (existingPending != null)
            {
                return BadRequest(new { error = "Email already pending approval" });
            }

            try
            {
                // Create pending player entry (NOT a real account yet)
                var pendingPlayer = new PendingPlayer
                {
                    Id = Guid.NewGuid(),
                    FullName = request.FullName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = request.Password, // Store raw password temporarily (will be hashed on approval)
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.PendingPlayers.Add(pendingPlayer);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    message = "Registration successful. Please wait for admin approval.",
                    pendingId = pendingPlayer.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to process registration: {ex.Message}" });
            }
        }

        // POST: api/auth/login
        // Allows existing users to log in
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Basic validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { error = "Invalid email or password" });
            }

            // Try to sign in with email and password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (!result.Succeeded)
            {
                return BadRequest(new { error = "Invalid email or password" });
            }

            // Check if user is admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin)
            {
                // Check if player record exists and is active (only for non-admin users)
                var player = await _dbContext.Players.FindAsync(user.PlayerId);
                if (player == null || !player.IsActive)
                {
                    return BadRequest(new { error = "Please wait for the Admin to approve your account." });
                }
            }

            // Generate JWT token
            var token = GenerateJwtToken(user, isAdmin);

            // Password is correct - return success with JWT token
            return Ok(new
            {
                message = "Login successful",
                token = token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    phoneNumber = user.PhoneNumber,
                    playerId = user.PlayerId,
                    isAdmin = isAdmin
                }
            });
        }

        // GET: api/auth/me
        // Returns current logged-in user info (requires authentication)
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Get the currently logged-in user's ID from the claims
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userIdGuid.ToString());
            if (user == null)
            {
                return NotFound();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            return Ok(new
            {
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    phoneNumber = user.PhoneNumber,
                    playerId = user.PlayerId,
                    isAdmin = isAdmin
                }
            });
        }

        // GET: api/auth/admin/pending-players
        // Returns all pending players waiting for admin approval
        [HttpGet("admin/pending-players")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingPlayers()
        {
            var pendingPlayers = await _dbContext.PendingPlayers
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            var result = pendingPlayers.Select(p => new
            {
                id = p.Id,
                fullName = p.FullName,
                email = p.Email,
                phoneNumber = p.PhoneNumber,
                createdAt = p.CreatedAt
            });

            return Ok(new { pendingPlayers = result });
        }

        // POST: api/auth/admin/approve-player/{id}
        // Approves a pending player and creates their account
        [HttpPost("admin/approve-player/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePlayer(Guid id)
        {
            var pendingPlayer = await _dbContext.PendingPlayers.FindAsync(id);
            if (pendingPlayer == null)
            {
                return NotFound(new { error = "Pending player not found" });
            }

            try
            {
                // Create the ApplicationUser account
                var newUser = new ApplicationUser
                {
                    UserName = pendingPlayer.Email,
                    Email = pendingPlayer.Email,
                    FullName = pendingPlayer.FullName,
                    PhoneNumber = pendingPlayer.PhoneNumber
                };

                var result = await _userManager.CreateAsync(newUser, pendingPlayer.PasswordHash);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { error = $"Failed to create user account: {errors}" });
                }

                // Create the Player record (marked as Active)
                var newPlayer = new Player
                {
                    Id = Guid.NewGuid(),
                    FullName = pendingPlayer.FullName,
                    Email = pendingPlayer.Email,
                    PhoneNumber = pendingPlayer.PhoneNumber,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Add Player to context and save BEFORE linking to ApplicationUser
                _dbContext.Players.Add(newPlayer);

                // Remove from pending players
                _dbContext.PendingPlayers.Remove(pendingPlayer);
                await _dbContext.SaveChangesAsync();

                // NOW link the ApplicationUser to the created Player
                newUser.PlayerId = newPlayer.Id;
                await _userManager.UpdateAsync(newUser);

                // Assign Player role to the new user
                await _userManager.AddToRoleAsync(newUser, "Player");

                return Ok(new
                {
                    message = "Player approved successfully",
                    player = new
                    {
                        id = newPlayer.Id,
                        fullName = newPlayer.FullName,
                        email = newPlayer.Email,
                        phoneNumber = newPlayer.PhoneNumber,
                        isActive = newPlayer.IsActive,
                        createdAt = newPlayer.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to approve player: {ex.Message}" });
            }
        }

        // GET: api/auth/admin/players
        // Returns all approved players (excludes admin accounts)
        [HttpGet("admin/players")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPlayers()
        {
            // Get all admin users
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminPlayerIds = adminUsers
                .Where(u => u.PlayerId.HasValue)
                .Select(u => u.PlayerId.Value)
                .ToList();

            // Get all players except those linked to admin accounts
            var players = await _dbContext.Players
                .Where(p => !adminPlayerIds.Contains(p.Id))
                .OrderBy(p => p.FullName)
                .ToListAsync();

            var result = players.Select(p => new
            {
                id = p.Id,
                fullName = p.FullName,
                email = p.Email,
                phoneNumber = p.PhoneNumber,
                isActive = p.IsActive,
                createdAt = p.CreatedAt
            });

            return Ok(new { players = result });
        }

        // POST: api/auth/admin/toggle-player/{id}/active
        // Toggles player active/inactive status
        [HttpPost("admin/toggle-player/{id}/active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TogglePlayerActive(Guid id)
        {
            var player = await _dbContext.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound(new { error = "Player not found" });
            }

            try
            {
                player.IsActive = !player.IsActive;
                _dbContext.Players.Update(player);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    message = player.IsActive ? "Player activated" : "Player deactivated",
                    player = new
                    {
                        id = player.Id,
                        fullName = player.FullName,
                        email = player.Email,
                        phoneNumber = player.PhoneNumber,
                        isActive = player.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to toggle player status: {ex.Message}" });
            }
        }
    }

    // Request models for API input
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required")]
        public string PhoneNumber { get; set; } = null!;
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
    }
}

// JWT Settings - used to generate tokens
public class JwtSettings
{
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
}
