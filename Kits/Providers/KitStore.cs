using JetBrains.Annotations;
using Kits.API;
using Kits.Databases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.Core.Helpers;
using OpenMod.Core.Plugins.Events;
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
        private readonly ILogger<KitStore> m_Logger;

        private IKitDatabase m_Database = null!;
        private IDisposable? m_ConfigurationChangedWatcher;

        public KitStore(Kits plugin, IPermissionRegistry permissionRegistry, ILogger<KitStore> logger,
            IEventBus eventBus)
        {
            m_Plugin = plugin;
            m_PermissionRegistry = permissionRegistry;
            m_Logger = logger;

            AsyncHelper.RunSync(ParseLoadDatabase);

            m_ConfigurationChangedWatcher =
                eventBus.Subscribe<PluginConfigurationChangedEvent>(plugin, PluginConfigurationChanged);
        }

        private Task PluginConfigurationChanged(IServiceProvider serviceprovider, object? sender,
            PluginConfigurationChangedEvent @event)
        {
            return @event.Plugin != m_Plugin ? Task.CompletedTask : ParseLoadDatabase();
        }

        private async Task ParseLoadDatabase()
        {
            m_Database = (m_Plugin.Configuration["database:connectionType"].ToLower() switch
            {
                "mysql" => new MySQLKitDatabase(m_Plugin),
                "datastore" => new DataStoreKitDatabase(m_Plugin),
                _ => null
            })!;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (m_Database == null)
            {
                m_Database = new DataStoreKitDatabase(m_Plugin);
                m_Logger.LogWarning(
                    $"Unable to parse {m_Plugin.Configuration["database:connectionType"]}. Setting to default: `datastore`");
            }

            await m_Database!.LoadDatabaseAsync();
            await RegisterPermissionsAsync();
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
            if (kit?.Name is not null)
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
            m_ConfigurationChangedWatcher?.Dispose();
            (m_Database as IDisposable)?.Dispose();
        }
    }
}