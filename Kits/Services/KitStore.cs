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
    public class KitStore : IKitStore, IAsyncDisposable
    {
        public const string c_KitsKey = "kits";

        private readonly Kits m_Plugin;
        private readonly IPermissionRegistry m_PermissionRegistry;
        private readonly ILogger<KitStore> m_Logger;
        private readonly IServiceProvider m_Provider;
        private readonly IDisposable? m_ConfigurationChangedWatcher;

        private IKitDatabase m_Database = null!;

        public IKitDatabase Database => m_Database;

        public KitStore(Kits plugin, IPermissionRegistry permissionRegistry, ILogger<KitStore> logger,
            IEventBus eventBus, IServiceProvider provider)
        {
            m_Plugin = plugin;
            m_PermissionRegistry = permissionRegistry;
            m_Logger = logger;
            m_Provider = provider;
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
            var type = m_Plugin.Configuration["database:connectionType"];
            m_Database = type.ToLower() switch
            {
                "mysql" => new MySqlKitDatabase(m_Provider),
                "datastore" => new DataStoreKitDatabase(m_Plugin),
                _ => null!
            };

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (m_Database == null)
            {
                m_Database = new DataStoreKitDatabase(m_Plugin);
                m_Logger.LogWarning("Unable to parse {DatabaseType}. Setting to default: `datastore`", type);
            }
            else
            {
                m_Logger.LogInformation("Datastore type set to `{DatabaseType}`", type);
            }

            await m_Database.LoadDatabaseAsync();
            await RegisterPermissionsAsync();
        }

        public Task<IReadOnlyCollection<Kit>> GetKitsAsync()
        {
            return m_Database.GetKitsAsync();
        }

        public async Task AddKitAsync(Kit kit)
        {
            if (kit?.Name == null || kit?.Items == null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            if (await m_Database.AddKitAsync(kit))
            {
                RegisterPermission(kit.Name);
            }
        }

        public async Task<Kit?> FindKitByNameAsync(string kitName)
        {
            if (string.IsNullOrEmpty(kitName))
            {
                throw new ArgumentException($"'{nameof(kitName)}' cannot be null or empty.", nameof(kitName));
            }

            var kit = await m_Database.FindKitByNameAsync(kitName);
            if (kit?.Name is not null)
            {
                RegisterPermission(kit.Name);
            }

            return kit;
        }

        public Task RemoveKitAsync(string kitName)
        {
            if (string.IsNullOrEmpty(kitName))
            {
                return Task.FromException(new ArgumentException(
                    $"'{nameof(kitName)}' cannot be null or empty.", nameof(kitName)));
            }

            return m_Database.RemoveKitAsync(kitName);
        }

        protected virtual async Task RegisterPermissionsAsync()
        {
            foreach (var kit in await m_Database.GetKitsAsync())
            {
                if (kit.Name != null)
                {
                    RegisterPermission(kit.Name);
                }
            }
        }

        protected virtual void RegisterPermission(string kitName)
        {
            m_PermissionRegistry.RegisterPermission(m_Plugin, "kits." + kitName.ToLower());
        }

        public ValueTask DisposeAsync()
        {
            m_ConfigurationChangedWatcher?.Dispose();
            return new(m_Database.DisposeSyncOrAsync());
        }
    }
}