using JetBrains.Annotations;
using Kits.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Commands;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Providers
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
    [UsedImplicitly]
    public class KitManager : IKitManager
    {
        private readonly ILogger<Kits> m_Logger;
        private readonly IEconomyProvider m_EconomyProvider;
        private readonly IKitCooldownStore m_KitCooldownStore;
        private readonly IKitStore m_KitStore;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly Kits m_Plugin;
        private readonly IItemSpawner m_ItemSpawner;

        public KitManager(ILogger<Kits> logger, IEconomyProvider economyProvider,
            IKitCooldownStore kitCooldownStore, IKitStore kitStore, IStringLocalizer stringLocalizer,
            IPermissionChecker permissionChecker, Kits plugin, IItemSpawner itemSpawner)
        {
            m_Logger = logger;
            m_EconomyProvider = economyProvider;
            m_KitCooldownStore = kitCooldownStore;
            m_KitStore = kitStore;
            m_StringLocalizer = stringLocalizer;
            m_PermissionChecker = permissionChecker;
            m_Plugin = plugin;
            m_ItemSpawner = itemSpawner;
        }

        public async Task GiveKitAsync(IPlayerUser user, string name)
        {
            if (user.Player is not IHasInventory inventory)
            {
                throw new UserFriendlyException("IPlayer doesn't have compatibility IHasInventory");
            }

            //var kits = await m_KitStore.GetKits();

            var kit = await m_KitStore.GetKit(name);
            if (kit == null)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:notFound", new { Name = name }]);
            }
            if (await m_PermissionChecker.CheckPermissionAsync(user,
                $"{m_Plugin.OpenModComponentId}:{KitStore.c_KitsKey}.{kit.Name}") != PermissionGrantResult.Grant)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:noPermission", new { Kit = kit }]);
            }

            var cooldown = await m_KitCooldownStore.GetLastCooldown(user, name);
            if (cooldown != null)
            {
                if (cooldown.Value.TotalSeconds < kit.Cooldown)
                {
                    throw new UserFriendlyException(m_StringLocalizer["commands:kit:cooldown",
                        new { Kit = kit, Cooldown = kit.Cooldown - cooldown.Value.TotalSeconds }]);
                }
            }
            await m_KitCooldownStore.RegisterCooldown(user, name, DateTime.Now);

            var balance = await m_EconomyProvider.GetBalanceAsync(user.Id, user.Type);
            if (kit.Cost != 0 && balance < kit.Cost)
            {
                var money = kit.Cost - balance;
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:noMoney",
                    new
                        {
                            Kit = kit,
                            Money = money,
                            MoneyName = m_EconomyProvider.CurrencyName,
                            MoneySymbol = m_EconomyProvider.CurrencySymbol
                        }]);
            }

            if (kit.Cost is not null && kit.Cost != 0)
            {
                await m_EconomyProvider.UpdateBalanceAsync(user.Id, user.Type, -kit.Cost.Value,
                    m_StringLocalizer["commands:kit:balanceUpdateReason:buy", new { Kit = kit }]);
            }
            if (kit.Money is not null && kit.Money != 0)
            {
                await m_EconomyProvider.UpdateBalanceAsync(user.Id, user.Type, kit.Money.Value,
                    m_StringLocalizer["commands:kit:balanceUpdateReason:got", new { Kit = kit }]);
            }

            foreach (var item in kit.Items!)
            {
                try
                {
                    var inventoryItem = await m_ItemSpawner.GiveItemAsync(inventory.Inventory!, item.ItemAssetId, item.State);
                    if (inventoryItem == null)
                    {
                        m_Logger.LogError($"Item {item.ItemAssetId} was unable to give to player {user.FullActorName})");
                    }
                }
                catch (Exception e)
                {
                    m_Logger.LogError(e, $"Item {item.ItemAssetId} was unable to give to player {user.FullActorName})");
                }
            }

            await user.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);
        }

        public async Task<IReadOnlyCollection<Kit>> GetAvailablePlayerKits(IPlayerUser player)
        {
            var list = new List<Kit>();
            foreach (var kit in await m_KitStore.GetKits())
            {
                if (await m_PermissionChecker.CheckPermissionAsync(player,
                    $"{m_Plugin.OpenModComponentId}:{KitStore.c_KitsKey}.{kit.Name}") == PermissionGrantResult.Grant)
                {
                    list.Add(kit);
                }
            }
            return list;
        }
    }
}
