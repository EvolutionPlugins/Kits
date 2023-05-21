using Autofac;
using Cysharp.Text;
using Kits.API;
using Kits.API.Cooldowns;
using Kits.API.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Commands;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Plugins;
using OpenMod.API.Prioritization;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Kits.Services;

[ServiceImplementation(Lifetime = ServiceLifetime.Transient, Priority = Priority.Lowest)]
public class KitManager : IKitManager
{
    private static readonly string s_PrefixPermissionKits;
    static KitManager()
    {
        s_PrefixPermissionKits = typeof(KitManager).Assembly.GetCustomAttribute<PluginMetadataAttribute>().Id + ":kits.";
    }

    private readonly IEconomyProvider m_EconomyProvider;
    private readonly IKitCooldownStore m_KitCooldownStore;
    private readonly IKitStore m_KitStore;
    private readonly IPermissionChecker m_PermissionChecker;
    private readonly IServiceProvider m_ServiceProvider;
    private readonly ILogger<KitManager> m_Logger;
    private readonly IStringLocalizer? m_StringLocalizer;

    public KitManager(IEconomyProvider economyProvider, IKitCooldownStore kitCooldownStore, IKitStore kitStore, IPermissionChecker permissionChecker,
        IPluginAccessor<KitsPlugin> pluginAccessor, IServiceProvider serviceProvider, ILogger<KitManager> logger)
    {
        m_EconomyProvider = economyProvider;
        m_KitCooldownStore = kitCooldownStore;
        m_KitStore = kitStore;
        m_PermissionChecker = permissionChecker;
        m_ServiceProvider = serviceProvider;
        m_Logger = logger;

        m_StringLocalizer = pluginAccessor.Instance?.LifetimeScope.Resolve<IStringLocalizer>();
    }

    public async Task GiveKitAsync(IPlayerUser user, string name, ICommandActor? instigator = null, bool forceGiveKit = false)
    {
        var kit = await m_KitStore.FindKitByNameAsync(name);
        if (kit == null)
        {
            throw new UserFriendlyException(m_StringLocalizer!["commands:kit:notFound", new { Name = name }]);
        }

        await GiveKitAsync(user, kit, instigator, forceGiveKit);
    }

    public async Task GiveKitAsync(IPlayerUser user, Kit kit, ICommandActor? instigator = null, bool forceGiveKit = false)
    {
        if (!forceGiveKit && await CheckPermissionKitAsync(user, kit.Name) != PermissionGrantResult.Grant)
        {
            throw new UserFriendlyException(m_StringLocalizer!["commands:kit:noPermission", new { Kit = kit }]);
        }

        var cooldown = await m_KitCooldownStore.GetLastCooldownAsync(user, kit.Name);
        if (!forceGiveKit && cooldown != null && cooldown.Value.TotalSeconds < kit.Cooldown)
        {
            throw new UserFriendlyException(m_StringLocalizer!["commands:kit:cooldown",
                new { Kit = kit, Cooldown = kit.Cooldown - cooldown.Value.TotalSeconds }]);
        }

        if (!forceGiveKit && kit.Cost != 0)
        {
            await m_EconomyProvider.UpdateBalanceAsync(user.Id, user.Type, -kit.Cost,
                m_StringLocalizer!["commands:kit:balanceUpdateReason:buy", new { Kit = kit }]);
        }

        await m_KitCooldownStore.RegisterCooldownAsync(user, kit.Name, DateTime.Now);

        await kit.GiveKitToPlayer(user, m_ServiceProvider);

        await user.PrintMessageAsync(m_StringLocalizer!["commands:kit:success", new { Kit = kit }]);

        if (instigator != null)
        {
            await instigator.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);
        }
    }

    public async Task<IReadOnlyCollection<Kit>> GetAvailableKitsForPlayerAsync(IPlayerUser player)
    {
        var sw = m_Logger.IsEnabled(LogLevel.Debug) ? Stopwatch.StartNew() : null;

        var list = new List<Kit>();
        foreach (var kit in await m_KitStore.GetKitsAsync())
        {
            if (await CheckPermissionKitAsync(player, kit.Name) == PermissionGrantResult.Grant)
            {
                list.Add(kit);
            }
        }

        m_Logger.LogDebug("Get available kits for user was take: {SpentMs}ms", sw?.ElapsedMilliseconds ?? 0);

        return list;
    }

    private Task<PermissionGrantResult> CheckPermissionKitAsync(IPermissionActor actor, string kitName)
    {
        var permission = ZString.Concat(s_PrefixPermissionKits, kitName);
        return m_PermissionChecker.CheckPermissionAsync(actor, permission);
    }
}