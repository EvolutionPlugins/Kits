﻿using Autofac;
using Kits.API.Cooldowns;
using Kits.Cooldowns.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.Core.Helpers;
using OpenMod.Core.Permissions;
using System;
using System.Threading.Tasks;

[assembly: RegisterPermission("nocooldown", Description = "Allows use kit without waiting for cooldown")]

namespace Kits.Cooldowns;

[PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
public class KitCooldownStore : IKitCooldownStore, IAsyncDisposable
{
    private const string c_NoCooldownPermission = "nocooldown";

    private readonly KitsPlugin m_Plugin;
    private readonly IPermissionChecker m_PermissionChecker;
    private readonly IOptions<KitCooldownOptions> m_Options;
    private readonly IServiceProvider m_ServiceProvider;
    private readonly ILogger<KitCooldownStore> m_Logger;

    private IKitCooldownStoreProvider m_CooldownProvider = null!;

    public KitCooldownStore(KitsPlugin plugin, IPermissionChecker permissionChecker, IOptions<KitCooldownOptions> options, IServiceProvider serviceProvider,
        ILogger<KitCooldownStore> logger)
    {
        m_Plugin = plugin;
        m_PermissionChecker = permissionChecker;
        m_Options = options;
        m_ServiceProvider = serviceProvider;
        m_Logger = logger;

        AsyncHelper.RunSync(InitAsync);
    }

    private async Task InitAsync()
    {
        var configuration = m_Plugin.LifetimeScope.Resolve<IConfiguration>();
        var type = configuration["cooldowns:connectionType"] ?? string.Empty;
        var providerType = m_Options.Value.FindType(type);

        if (providerType != null)
        {
            m_Logger.LogInformation("Cooldown store type set to `{DatabaseType}`", type);
            try
            {
                m_CooldownProvider = (ActivatorUtilities.CreateInstance(m_ServiceProvider, providerType) as IKitCooldownStoreProvider)!;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to initialize {CooldownStoreName}. Defaulting to `datastore`", providerType.Name);
            }
        }
        else
        {
            m_Logger.LogWarning("Unable to parse {DatabaseType}. Setting to default: `datastore`", type);
        }

        m_CooldownProvider ??= new DataStoreKitCooldownStoreProvider(m_Plugin);

        await m_CooldownProvider.InitAsync();
    }

    public async Task<TimeSpan?> GetLastCooldownAsync(IPermissionActor actor, string kitName)
    {
        return await HasNoCooldownPermission(actor) ? null : await m_CooldownProvider.GetLastCooldownAsync(actor, kitName);
    }

    public async Task RegisterCooldownAsync(IPermissionActor actor, string kitName, DateTime time)
    {
        if (await HasNoCooldownPermission(actor))
        {
            return;
        }

        await m_CooldownProvider.RegisterCooldownAsync(actor, kitName, time);
    }

    private async Task<bool> HasNoCooldownPermission(IPermissionActor actor)
    {
        return await m_PermissionChecker.CheckPermissionAsync(actor, c_NoCooldownPermission) is PermissionGrantResult.Grant;
    }

    public ValueTask DisposeAsync()
    {
        return m_CooldownProvider == null ? new() : new(m_CooldownProvider.DisposeSyncOrAsync());
    }
}