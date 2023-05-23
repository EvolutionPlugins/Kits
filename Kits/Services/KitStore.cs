using Autofac;
using Cysharp.Text;
using Kits.API;
using Kits.API.Databases;
using Kits.API.Models;
using Kits.Databases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Plugins;
using OpenMod.Core.Helpers;
using OpenMod.Core.Plugins.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Services;

[ServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
public class KitStore : IKitStore, IAsyncDisposable
{
    private readonly IPermissionRegistry m_PermissionRegistry;
    private readonly ILogger<KitStore> m_Logger;
    private readonly IEventBus m_EventBus;
    private readonly IOptions<KitStoreOptions> m_Options;
    private readonly IRuntime m_Runtime;

    private IOpenModPlugin? m_Plugin;
    private IDisposable? m_ConfigurationChangedWatcher;

    public IKitStoreProvider DatabaseProvider { get; private set; } = null!;

    public KitStore(IPermissionRegistry permissionRegistry, ILogger<KitStore> logger,
        IEventBus eventBus, IOptions<KitStoreOptions> options, IRuntime runtime)
    {
        m_PermissionRegistry = permissionRegistry;
        m_Logger = logger;
        m_EventBus = eventBus;
        m_Options = options;
        m_Runtime = runtime;
        ScheduleWaitForPluginLoading();
    }

    private void ScheduleWaitForPluginLoading()
    {
        IDisposable eventHandler = NullDisposable.Instance;

        eventHandler = m_EventBus.Subscribe<PluginActivatingEvent>(m_Runtime, async (_, _, @event) =>
        {
            if (@event.Plugin is not KitsPlugin)
            {
                return;
            }

            m_Plugin = @event.Plugin;
            eventHandler.Dispose();
            await InitAsync();
        });
    }

    public async Task InitAsync()
    {
        await ParseLoadDatabase();

        m_ConfigurationChangedWatcher = m_EventBus.Subscribe<PluginConfigurationChangedEvent>(m_Runtime, PluginConfigurationChangedAsync);
    }

    private Task PluginConfigurationChangedAsync(IServiceProvider _, object? __,
        PluginConfigurationChangedEvent @event)
    {
        if (@event.Plugin is not KitsPlugin || m_Plugin == null)
        {
            return Task.CompletedTask;
        }

        return ParseLoadDatabase();
    }

    private async Task ParseLoadDatabase()
    {
        var configuration = m_Plugin!.LifetimeScope.Resolve<IConfiguration>();
        var type = configuration["database:connectionType"] ?? string.Empty;
        var databaseType = m_Options.Value.FindType(type);

        if (databaseType != null)
        {
            m_Logger.LogDebug("Database type set to `{DatabaseType}`", type);
            try
            {
                var serviceProvider = m_Plugin.LifetimeScope.Resolve<IServiceProvider>();
                DatabaseProvider = (IKitStoreProvider)ActivatorUtilities.CreateInstance(serviceProvider, databaseType);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to initialize {DatabaseName}. Defaulting to `datastore`", databaseType.Name);
            }
        }
        else
        {
            m_Logger.LogWarning("Unable to parse {DatabaseType}. Setting to default: `datastore`", type);
        }

        DatabaseProvider ??= new DataStoreKitStoreProvider(m_Plugin.LifetimeScope);
        try
        {
            await DatabaseProvider.InitAsync();
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to initialize {DatabaseProviderName}. Resetting to the default store provider",
                DatabaseProvider.GetType().Name);

            if (DatabaseProvider is not DataStoreKitStoreProvider)
            {
                // dispose safely
                try
                {
                    await DatabaseProvider.DisposeSyncOrAsync();
                }
                catch (Exception ex2)
                {
                    m_Logger.LogError(ex2, "Failed to dispose {DatabaseProviderName}", DatabaseProvider.GetType().Name);
                }

                DatabaseProvider = new DataStoreKitStoreProvider(m_Plugin.LifetimeScope);
                await DatabaseProvider.InitAsync();
            }
        }
        await RegisterPermissionsAsync();
    }

    public Task<IReadOnlyCollection<Kit>> GetKitsAsync()
    {
        return DatabaseProvider.GetKitsAsync();
    }

    public async Task AddKitAsync(Kit kit)
    {
        if (kit?.Name == null || kit?.Items == null)
        {
            throw new ArgumentNullException(nameof(kit));
        }

        await DatabaseProvider.AddKitAsync(kit);
        RegisterPermission(kit.Name);
    }

    public async Task<Kit?> FindKitByNameAsync(string kitName)
    {
        if (string.IsNullOrEmpty(kitName))
        {
            throw new ArgumentException($"'{nameof(kitName)}' cannot be null or empty.", nameof(kitName));
        }

        var kit = await DatabaseProvider.FindKitByNameAsync(kitName);
        if (kit?.Name is not null)
        {
            // kit was maybe manually added, registering the permission for safety reason
            RegisterPermission(kit.Name);
        }

        return kit;
    }

    public Task RemoveKitAsync(string kitName)
    {
        return string.IsNullOrEmpty(kitName)
            ? Task.FromException(new ArgumentException(
                $"'{nameof(kitName)}' cannot be null or empty.", nameof(kitName)))
            : DatabaseProvider.RemoveKitAsync(kitName);
    }

    public Task UpdateKitAsync(Kit kit)
    {
        return DatabaseProvider.UpdateKitAsync(kit);
    }

    public Task<bool> IsKitExists(string name)
    {
        return DatabaseProvider.IsKitExists(name);
    }

    protected virtual async Task RegisterPermissionsAsync()
    {
        foreach (var kit in await DatabaseProvider.GetKitsAsync())
        {
            if (kit.Name != null)
            {
                RegisterPermission(kit.Name);
            }
        }
    }

    protected virtual void RegisterPermission(string kitName)
    {
        m_PermissionRegistry.RegisterPermission(m_Plugin!, ZString.Concat("kits.", kitName.ToLower()),
            $"Grants access to the {kitName} kit");
    }

    public async ValueTask DisposeAsync()
    {
        m_ConfigurationChangedWatcher?.Dispose();
        m_ConfigurationChangedWatcher = null;

        if (DatabaseProvider != null)
        {
            await DatabaseProvider.DisposeSyncOrAsync();
        }

        DatabaseProvider = null!;
    }
}