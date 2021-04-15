using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMod.API.Ioc;
using OpenMod.Extensions.Games.Abstractions.Players;

namespace Kits.API
{
    [Service]
    public interface IKitManager
    {
        Task GiveKitAsync(IPlayerUser user, string name);

        Task<IReadOnlyCollection<Kit>> GetAvailablePlayerKits(IPlayerUser player);
    }
}