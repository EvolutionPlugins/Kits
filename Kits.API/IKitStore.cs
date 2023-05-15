using Kits.API.Databases;
using Kits.API.Models;
using OpenMod.API.Ioc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.API;

/// <summary>
/// The service for getting kits
/// </summary>
[Service]
public interface IKitStore
{
    /// <summary>
    /// Gets the kits from <see cref="IKitStoreProvider"/>
    /// </summary>
    /// <returns>Kits list</returns>
    Task<IReadOnlyCollection<Kit>> GetKitsAsync();

    Task<Kit?> FindKitByNameAsync(string name);

    Task<bool> IsKitExists(string name);

    Task AddKitAsync(Kit kit);

    Task RemoveKitAsync(string name);

    Task UpdateKitAsync(Kit kit);

    /// <summary>
    /// Initializing the kit store and <see cref="IKitStoreProvider"/>.
    /// <b>Should not be called from plugins</b>
    /// </summary>
    Task InitAsync();
}