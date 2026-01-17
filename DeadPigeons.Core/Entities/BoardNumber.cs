using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadPigeons.Core.Entities
{
    public class BoardNumber
    {
        public Guid Id { get; set; }

        public Guid BoardId { get; set; }
        public Board? Board { get; set; }

        public int Number { get; set; } // The selected number (1-16)
    }
}
