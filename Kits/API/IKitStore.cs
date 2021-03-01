using OpenMod.API.Ioc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.API
{
    [Service]
    public interface IKitStore
    {
        Task<IReadOnlyCollection<Kit>> GetKits();

        Task<Kit?> GetKit(string kitName);

        Task AddKit(Kit kit);

        Task RemoveKit(string kitName);
    }
}
