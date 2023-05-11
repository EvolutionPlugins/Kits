using Cysharp.Text;
using Kits.API;
using Kits.API.Databases;
using Kits.API.Models;
using Kits.Databases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.Core.Helpers;
using OpenMod.Core.Plugins.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Services;

[PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
public class KitStore : IKitStore, IAsyncDisposable
{
    public const string c_KitsKey = "kits";

    private readonly KitsPlugin m_Plugin;
    private readonly IPermissionRegistry m_PermissionRegistry;
    private readonly ILogger<KitStore> m_Logger;
    private readonly IOptions<KitDatabaseOptions> m_Options;
    private readonly IServiceProvider m_ServiceProvider;
    private readonly IDisposable? m_ConfigurationChangedWatcher;

    public IKitDatabaseProvider Database { get; private set; } = null!;

    public KitStore(KitsPlugin plugin, IPermissionRegistry permissionRegistry, ILogger<KitStore> logger,
        IEventBus eventBus, IOptions<KitDatabaseOptions> options, IServiceProvider serviceProvider)
    {
        m_Plugin = plugin;
        m_PermissionRegistry = permissionRegistry;
        m_Logger = logger;
        m_Options = options;
        m_ServiceProvider = serviceProvider;
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
        var databaseType = m_Options.Value.GetPreferredDatabase(type);

        if (databaseType == null)
        {
            m_Logger.LogWarning("Unable to parse {DatabaseType}. Setting to default: `datastore`", type);
            Database = new DataStoreKitDataStore(m_Plugin.LifetimeScope);
        }
        else
        {
            m_Logger.LogInformation("Database type set to `{DatabaseType}`", type);
            Database = (ActivatorUtilities.CreateInstance(m_ServiceProvider, databaseType) as IKitDatabaseProvider)
                ?? throw new Exception($"Failed to create database provider of type {databaseType.Name}");
        }

        await Database.LoadDatabaseAsync();
        await RegisterPermissionsAsync();
    }

    public Task<IReadOnlyCollection<Kit>> GetKitsAsync()
    {
        return Database.GetKitsAsync();
    }

    public async Task AddKitAsync(Kit kit)
    {
        if (kit?.Name == null || kit?.Items == null)
        {
            throw new ArgumentNullException(nameof(kit));
        }

        if (await Database.AddKitAsync(kit))
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

        var kit = await Database.FindKitByNameAsync(kitName);
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

        return Database.RemoveKitAsync(kitName);
    }

    protected virtual async Task RegisterPermissionsAsync()
    {
        foreach (var kit in await Database.GetKitsAsync())
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

        if (Database == null)
        {
            return new();
        }

        return new(Database.DisposeSyncOrAsync());
    }
}