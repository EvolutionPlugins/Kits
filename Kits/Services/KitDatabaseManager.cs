using Autofac;
using JetBrains.Annotations;
using Kits.API;
using Kits.API.Database;
using Kits.Databases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Core.Helpers;
using OpenMod.Core.Ioc;
using OpenMod.Core.Plugins.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Providers
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
    [UsedImplicitly]
    public class KitDatabaseManager : IKitDatabaseManager, IAsyncDisposable
    {
        public const string c_KitsKey = "kits";

        private readonly IPermissionRegistry m_PermissionRegistry;
        private readonly ILogger<KitDatabaseManager> m_Logger;
        private readonly IEventBus m_EventBus;
        private readonly KitDatabaseOptions m_Options;

        private IKitDatabaseProvider m_Database = null!;
        private ILifetimeScope m_LifetimeScope = null!;
        private IDisposable? m_ConfigurationChangedWatcher;
        private Kits? m_Plugin;

        public IKitDatabaseProvider Database => m_Database;

        public KitDatabaseManager(IPermissionRegistry permissionRegistry, ILogger<KitDatabaseManager> logger,
            IEventBus eventBus, KitDatabaseOptions options)
        {
            m_PermissionRegistry = permissionRegistry;
            m_Logger = logger;
            m_EventBus = eventBus;
            m_Options = options;
        }

        private Task PluginConfigurationChanged(IServiceProvider serviceprovider, object? sender,
            PluginConfigurationChangedEvent @event)
        {
            return @event.Plugin != m_Plugin ? Task.CompletedTask : InitAsync(m_LifetimeScope);
        }

        public async Task InitAsync(ILifetimeScope lifetimeScope)
        {
            if (m_LifetimeScope != null && lifetimeScope != m_LifetimeScope)
            {
                return;
            }

            m_LifetimeScope = lifetimeScope;
            var configuration = lifetimeScope.Resolve<IConfiguration>();
            m_Plugin = lifetimeScope.Resolve<Kits>();

            var configType = configuration["database:connectionType"];
            var database = m_Options.GetPreferredDatabase(configType);

            if (database == null)
            {
                m_Database = ActivatorUtilitiesEx.CreateInstance<DataStoreKitDatabase>(lifetimeScope);
                m_Logger.LogWarning(
                    $"Unable to parse {configType}. Setting to default: `datastore`");
            }
            else
            {
                m_Logger.LogInformation($"Datastore type set to `{configType}`");
                m_Database = (IKitDatabaseProvider)ActivatorUtilitiesEx.CreateInstance(lifetimeScope, database);
            }

            m_ConfigurationChangedWatcher = m_EventBus.Subscribe<PluginConfigurationChangedEvent>(m_Plugin,
                PluginConfigurationChanged);

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
            m_PermissionRegistry.RegisterPermission(m_Plugin!, "kits." + kitName.ToLower());
        }

        public ValueTask DisposeAsync()
        {
            m_ConfigurationChangedWatcher?.Dispose();
            return new(m_Database.DisposeSyncOrAsync());
        }
    }
}