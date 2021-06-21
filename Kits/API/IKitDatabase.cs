using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.API
{
    public interface IKitDatabase
    {
        Task LoadDatabaseAsync();

        Task<IReadOnlyCollection<Kit>> GetKitsAsync();

        Task<Kit?> FindKitByName(string name);

        Task<bool> AddKitAsync(Kit kit);

        Task<bool> RemoveKitAsync(string name);

        Task<bool> UpdateKitAsync(Kit kit);
    }
}