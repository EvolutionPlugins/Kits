using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.API.Database
{
    public interface IKitDatabaseProvider
    {
        Task LoadDatabaseAsync();

        Task<IReadOnlyCollection<Kit>> GetKitsAsync();

        Task<Kit?> FindKitByNameAsync(string name);

        Task<bool> AddKitAsync(Kit kit);

        Task<bool> RemoveKitAsync(string name);

        Task<bool> UpdateKitAsync(Kit kit);
    }
}