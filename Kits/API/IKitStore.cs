using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMod.API.Ioc;

namespace Kits.API
{
    [Service]
    public interface IKitStore
    {
        IKitDatabase Database { get; }

        Task<IReadOnlyCollection<Kit>> GetKitsAsync();

        Task<Kit?> FindKitByNameAsync(string kitName);

        Task AddKitAsync(Kit kit);

        Task RemoveKitAsync(string kitName);
    }
}