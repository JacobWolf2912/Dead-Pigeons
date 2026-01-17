using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadPigeons.Core.Entities
{
    public class GameWinningNumbers
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public Game? Game { get; set; }
        public int Number1 { get; set; }
        public int Number2 { get; set; }
        public int Number3 { get; set; }
        public DateTime DrawnAt { get; set; } = DateTime.UtcNow;
    }
}
