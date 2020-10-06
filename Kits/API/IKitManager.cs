using OpenMod.API.Ioc;
using OpenMod.Extensions.Games.Abstractions.Players;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.API
{
    [Service]
    public interface IKitManager
    {
        Task GiveKit(IPlayer user, string name);

        Task AddKit(Kit kit);

        Task RemoveKit(string name);

        Task<IReadOnlyCollection<Kit>> GetKits();
    }
}
