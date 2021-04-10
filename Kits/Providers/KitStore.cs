using JetBrains.Annotations;
using Kits.API;
using Kits.Databases;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Providers
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    [UsedImplicitly]
    public class KitStore : IKitStore, IDisposable
    {
        public const string c_KitsKey = "kits";

        private readonly Kits m_Plugin;
        private readonly IPermissionRegistry m_PermissionRegistry;
        private readonly IKitDatabase m_Database;

        public KitStore(Kits plugin, IPermissionRegistry permissionRegistry)
        {
            m_Plugin = plugin;
            m_PermissionRegistry = permissionRegistry;
            m_Database = plugin.Configuration["database:connectionType"].ToLower() switch
            {
                "mysql" => new MySQLKitDatabase(plugin),
                "datastore" => new DataStoreKitDatabase(plugin),
                _ => throw new Exception()
            };
            AsyncHelper.RunSync(async () =>
            {
                await m_Database.LoadDatabaseAsync();
                await RegisterPermissionsAsync();
            });
        }

        public Task<IReadOnlyCollection<Kit>> GetKits()
        {
            return m_Database.GetKitsAsync();
        }

        public async Task AddKit(Kit kit)
        {
            if (!string.IsNullOrEmpty(kit.Name) && await m_Database.AddKitAsync(kit))
            {
                RegisterPermission(kit.Name!);
            }
        }

        public async Task<Kit?> GetKit(string kitName)
        {
            var kit = await m_Database.GetKitAsync(kitName);
            if(kit?.Name is not null)
            {
                RegisterPermission(kit.Name);
            }
            return kit;
        }

        public Task RemoveKit(string kitName)
        {
            return m_Database.RemoveKitAsync(kitName);
        }

        private async Task RegisterPermissionsAsync()
        {
            foreach (var kit in await m_Database.GetKitsAsync())
            {
                Console.WriteLine(kit.Name ?? "na");
                if (kit.Name != null)
                {
                    RegisterPermission(kit.Name);
                }
            }
        }

        private void RegisterPermission(string kitName)
        {
            m_PermissionRegistry.RegisterPermission(m_Plugin, "kits." + kitName.ToLower());
        }
        

        public void Dispose()
        {
            (m_Database as IDisposable)?.Dispose();
        }
    }
}
