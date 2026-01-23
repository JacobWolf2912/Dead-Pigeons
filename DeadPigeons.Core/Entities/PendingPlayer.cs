using System.ComponentModel.DataAnnotations;

namespace DeadPigeons.Core.Entities
{
    public class PendingPlayer
    {
        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        public string PasswordHash { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
    }
}
