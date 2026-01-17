using System;
using System.Collections.Generic;
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
        // WinningNumbers can be null until admin draws the numbers for this game
        public GameWinningNumbers? WinningNumbers { get; set; }
        public ICollection<Board> Boards { get; set; } = new List<Board>();
    }
}
