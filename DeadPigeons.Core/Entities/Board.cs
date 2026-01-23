using System.ComponentModel.DataAnnotations;

namespace DeadPigeons.Core.Entities
{
    public class Board
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PlayerId { get; set; }

        public Player? Player { get; set; }

        [Required]
        public Guid GameId { get; set; }

        public Game? Game { get; set; }

        [Required(ErrorMessage = "Field count is required")]
        [Range(5, 8, ErrorMessage = "Field count must be between 5 and 8")]
        public int FieldCount { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(20, 160, ErrorMessage = "Price must be between 20 and 160 DKK")]
        public decimal Price { get; set; }

        public bool IsWinningBoard { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // Numbers selected on this board (5-8 numbers between 1-16)
        public ICollection<BoardNumber> Numbers { get; set; } = new List<BoardNumber>();
    }
}
