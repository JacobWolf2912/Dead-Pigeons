using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeadPigeons.Core.Entities;

namespace DeadPigeons.Core.Interfaces
{
    public interface IPlayerRepository
    {
         Task<Player> AddAsync(Player player);
         Task<Player?> GetByIdAsync(Guid id);
         Task<IEnumerable<Player>> GetAllAsync();
         Task UpdateAsync(Player player);
    }
}
