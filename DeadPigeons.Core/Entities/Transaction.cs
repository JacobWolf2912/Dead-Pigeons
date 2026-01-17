using System.ComponentModel.DataAnnotations;

namespace DeadPigeons.Core.Entities
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PlayerId { get; set; }

        public Player? Player { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000 DKK")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "MobilePay transaction ID is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "MobilePay ID must be between 1 and 50 characters")]
        public string MobilePayTransactionId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        // Transaction status: pending (false) or approved (true)
        public bool IsApproved { get; set; } = false;
    }
}
