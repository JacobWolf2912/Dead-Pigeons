using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadPigeons.Core.Entities
{
    public class Game
    {
        public Guid Id { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime DrawTime { get; set; }
        public bool IsClosed { get; set; }
        [NotMapped]
        public string? Name { get; set; }
        // WinningNumbers can be null until admin draws the numbers for this game
        public GameWinningNumbers? WinningNumbers { get; set; }
        public ICollection<Board> Boards { get; set; } = new List<Board>();

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
    }
}
