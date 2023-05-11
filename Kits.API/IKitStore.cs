using Kits.API.Models;
using OpenMod.API.Ioc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.API;

[Service]
public interface IKitStore
{
    Task<IReadOnlyCollection<Kit>> GetKitsAsync();

    Task<Kit?> FindKitByNameAsync(string kitName);

    Task AddKitAsync(Kit kit);

    Task RemoveKitAsync(string kitName);
}