using OpenMod.API.Ioc;
using OpenMod.Extensions.Games.Abstractions.Players;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.API
{
    [Service]
    public interface IKitManager
    {
        Task GiveKitAsync(IPlayerUser playerUser, string name);

        Task AddKitAsync(Kit kit);

        Task<bool> RemoveKitAsync(string name);

        Task<IReadOnlyCollection<Kit>> GetRegisteredKitsAsync();
    }
}
