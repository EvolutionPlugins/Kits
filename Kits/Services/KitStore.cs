using Autofac;
using Cysharp.Text;
using Kits.API;
using Kits.API.Databases;
using Kits.API.Models;
using Kits.Databases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

[PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
public class KitStore : IKitStore, IAsyncDisposable
{
    private readonly IPermissionRegistry m_PermissionRegistry;
    private readonly ILogger<KitStore> m_Logger;
    private readonly IEventBus m_EventBus;
    private readonly IOptions<KitStoreOptions> m_Options;
    private readonly IServiceProvider m_ServiceProvider;

    private IOpenModPlugin m_Plugin = null!;
    private IDisposable? m_ConfigurationChangedWatcher;

    public IKitStoreProvider DatabaseProvider { get; private set; } = null!;

    public KitStore(IPermissionRegistry permissionRegistry, ILogger<KitStore> logger,
        IEventBus eventBus, IOptions<KitStoreOptions> options, IServiceProvider serviceProvider)
    {
        m_PermissionRegistry = permissionRegistry;
        m_Logger = logger;
        m_EventBus = eventBus;
        m_Options = options;
        m_ServiceProvider = serviceProvider;
    }

    public async Task InitAsync()
    {
        // already initialized
        if (m_Plugin != null)
        {
            return;
        }

        m_Plugin = m_ServiceProvider.GetRequiredService<KitsPlugin>();
        await ParseLoadDatabase();

        m_ConfigurationChangedWatcher = m_EventBus.Subscribe<PluginConfigurationChangedEvent>(m_Plugin, PluginConfigurationChangedAsync);
    }

    private Task PluginConfigurationChangedAsync(IServiceProvider _, object? __,
        PluginConfigurationChangedEvent @event)
    {
        return @event.Plugin != m_Plugin ? Task.CompletedTask : ParseLoadDatabase();
    }

    private async Task ParseLoadDatabase()
    {
        var configuration = m_Plugin.LifetimeScope.Resolve<IConfiguration>();
        var type = configuration["database:connectionType"] ?? string.Empty;
        var databaseType = m_Options.Value.FindType(type);

        if (databaseType != null)
        {
            m_Logger.LogInformation("Database type set to `{DatabaseType}`", type);
            try
            {
                DatabaseProvider = (ActivatorUtilities.CreateInstance(m_ServiceProvider, databaseType) as IKitStoreProvider)!;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to initialize {DatabaseName}. Defaulting to datastore", databaseType.Name);
            }
        }
        else
        {
            m_Logger.LogWarning("Unable to parse {DatabaseType}. Setting to default: `datastore`", type);
        }

        DatabaseProvider ??= new DataStoreKitStoreProvider(m_Plugin.LifetimeScope);

        await DatabaseProvider.InitAsync();
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
        if (string.IsNullOrEmpty(kitName))
        {
            return Task.FromException(new ArgumentException(
                $"'{nameof(kitName)}' cannot be null or empty.", nameof(kitName)));
        }

        return DatabaseProvider.RemoveKitAsync(kitName);
    }

    public async Task UpdateKitAsync(Kit kit)
    {

    }

    public Task<bool> IsKitExists(string name)
    {
        throw new NotImplementedException();
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
        m_PermissionRegistry.RegisterPermission(m_Plugin, ZString.Concat("kits.", kitName.ToLower()));
    }

    public ValueTask DisposeAsync()
    {
        m_ConfigurationChangedWatcher?.Dispose();

        if (DatabaseProvider == null)
        {
            return new();
        }

        return new(DatabaseProvider.DisposeSyncOrAsync());
    }
}