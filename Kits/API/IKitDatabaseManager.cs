using Autofac;
using Kits.API.Database;
using OpenMod.API.Ioc;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Kits.API
{
    [Service]
    public interface IKitDatabaseManager
    {
        IKitDatabaseProvider Database { get; }

        Task<IReadOnlyCollection<Kit>> GetKitsAsync();

        Task<Kit?> FindKitByNameAsync(string kitName);

        Task AddKitAsync(Kit kit);

        Task RemoveKitAsync(string kitName);

        /// <summary>
        /// Used for internal usage. Not for plugins
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task InitAsync(ILifetimeScope lifetimeScope);
    }
}