﻿using Kits.API;
using Kits.API.Cooldowns;
using Kits.API.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Services;

[PluginServiceImplementation(Lifetime = ServiceLifetime.Transient, Priority = Priority.Lowest)]
public class KitManager : IKitManager
{
    private readonly IEconomyProvider m_EconomyProvider;
    private readonly IKitCooldownStore m_KitCooldownStore;
    private readonly IKitStore m_KitStore;
    private readonly IStringLocalizer m_StringLocalizer;
    private readonly IPermissionChecker m_PermissionChecker;
    private readonly IServiceProvider m_ServiceProvider;

    public KitManager(IEconomyProvider economyProvider, IKitCooldownStore kitCooldownStore, IKitStore kitStore,
        IStringLocalizer stringLocalizer, IPermissionChecker permissionChecker, IServiceProvider serviceProvider)
    {
        m_EconomyProvider = economyProvider;
        m_KitCooldownStore = kitCooldownStore;
        m_KitStore = kitStore;
        m_StringLocalizer = stringLocalizer;
        m_PermissionChecker = permissionChecker;
        m_ServiceProvider = serviceProvider;
    }

    public async Task GiveKitAsync(IPlayerUser user, string name, ICommandActor? instigator = null, bool forceGiveKit = false)
    {
        var kit = await m_KitStore.FindKitByNameAsync(name);
        if (kit == null)
        {
            throw new UserFriendlyException(m_StringLocalizer["commands:kit:notFound", new { Name = name }]);
        }

        await GiveKitAsync(user, kit, instigator, forceGiveKit);
    }

    public async Task GiveKitAsync(IPlayerUser user, Kit kit, ICommandActor? instigator = null, bool forceGiveKit = false)
    {
        if (!forceGiveKit && await CheckPermissionKitAsync(user, kit.Name) != PermissionGrantResult.Grant)
        {
            throw new UserFriendlyException(m_StringLocalizer["commands:kit:noPermission", new { Kit = kit }]);
        }

        var cooldown = await m_KitCooldownStore.GetLastCooldownAsync(user, kit.Name);
        if (!forceGiveKit && cooldown != null && cooldown.Value.TotalSeconds < kit.Cooldown)
        {
            throw new UserFriendlyException(m_StringLocalizer["commands:kit:cooldown",
                new { Kit = kit, Cooldown = kit.Cooldown - cooldown.Value.TotalSeconds }]);
        }

        if (!forceGiveKit && kit.Cost != 0)
        {
            await m_EconomyProvider.UpdateBalanceAsync(user.Id, user.Type, -kit.Cost,
                m_StringLocalizer["commands:kit:balanceUpdateReason:buy", new { Kit = kit }]);
        }

        await m_KitCooldownStore.RegisterCooldownAsync(user, kit.Name, DateTime.Now);

        await kit.GiveKitToPlayer(user, m_ServiceProvider);

        await user.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);

        if (instigator != null)
        {
            await instigator.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);
        }
    }

    public async Task<IReadOnlyCollection<Kit>> GetAvailableKitsForPlayerAsync(IPlayerUser player)
    {
        var list = new List<Kit>();
        foreach (var kit in await m_KitStore.GetKitsAsync())
        {
            if (await CheckPermissionKitAsync(player, kit.Name) == PermissionGrantResult.Grant)
            {
                list.Add(kit);
            }
        }

        return list;
    }

    private Task<PermissionGrantResult> CheckPermissionKitAsync(IPermissionActor actor, string kitName)
    {
        var permission = "kits." + kitName;
        return m_PermissionChecker.CheckPermissionAsync(actor, permission);
    }
}