using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMod.API.Ioc;

namespace Kits.API
{
    [Service]
    public interface IKitStore
    {
        Task<IReadOnlyCollection<Kit>> GetKitsAsync();

        Task<Kit?> FindKitAsync(string kitName);

        Task AddKitAsyc(Kit kit);

        Task RemoveKitAsync(string kitName);
    }
}