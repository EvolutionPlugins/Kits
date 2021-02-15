using Autofac;
using Kits.API;
using Kits.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Commands;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Persistence;
using OpenMod.API.Plugins;
using OpenMod.API.Prioritization;
using OpenMod.Core.Helpers;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Providers
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
    public class KitManager : IKitManager, IDisposable
    {
#pragma warning disable IDE1006 // Naming Styles
        public const string COOLDOWNKEY = "cooldowns";
        public const string KITSKEY = "kits";
#pragma warning restore IDE1006 // Naming Styles

        private readonly IItemSpawner m_ItemSpawner;
        private readonly IPluginAccessor<Kits> m_PluginAccessor;
        private readonly IPermissionRegistry m_PermissionRegistry;
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly ILogger<KitManager> m_Logger;
        private readonly IEconomyProvider m_EconomyProvider;

        // make compiler happy
        private IStringLocalizer m_StringLocalizer = null!;
        private IDisposable m_KitsWatcher = null!;
        private IDisposable m_KitsCooldownWatcher = null!;
        private KitsCooldownData m_KitCooldownCache = null!;
        private KitsData m_KitCache = null!;

        private IDataStore DataStore => m_PluginAccessor.Instance!.DataStore;

        public KitManager(IItemSpawner itemSpawner, IPluginAccessor<Kits> pluginAccessor,
            IPermissionRegistry permissionRegistry, IPermissionChecker permissionChecker, ILogger<KitManager> logger,
            IEconomyProvider economyProvider)
        {
            m_ItemSpawner = itemSpawner;
            m_PluginAccessor = pluginAccessor;
            m_PermissionRegistry = permissionRegistry;
            m_PermissionChecker = permissionChecker;
            m_Logger = logger;
            m_EconomyProvider = economyProvider;
        }

        public async Task AddKitAsync(Kit kit)
        {
            var kits = await GetRegisteredKitsAsync();
            if (kits.Any(x => x.Name?.Equals(kit.Name, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                throw new UserFriendlyException("Kit with the same name already exists");
            }
            m_KitCache.Kits?.Add(kit);
            await DataStore.SaveAsync(KITSKEY, m_KitCache);
            RegisterPermissions();
        }

        public async Task<IReadOnlyCollection<Kit>> GetRegisteredKitsAsync()
        {
            if (m_KitCache == null)
            {
                await ReadData();
                m_KitsWatcher = DataStore.AddChangeWatcher(KITSKEY,
                    m_PluginAccessor.Instance!, () => AsyncHelper.RunSync(ReadData));
                m_KitsCooldownWatcher = DataStore.AddChangeWatcher(COOLDOWNKEY,
                    m_PluginAccessor.Instance, () => AsyncHelper.RunSync(ReadData));
            }
            return m_KitCache!.Kits!;
        }

        private async Task ReadData()
        {
            m_KitCache = await DataStore.LoadAsync<KitsData>(KITSKEY) ?? new KitsData
            {
                Kits = new List<Kit>()
            };
            m_KitCooldownCache = await DataStore.LoadAsync<KitsCooldownData>(COOLDOWNKEY) ?? new KitsCooldownData
            {
                KitsCooldown = new Dictionary<string, List<KitCooldownData>>()
            };
            RegisterPermissions();
        }

        private void RegisterPermissions()
        {
            foreach (var kit in m_KitCache!.Kits!)
            {
                m_Logger.LogDebug($"Register permission => Kits:{KITSKEY}.{kit.Name}");
                m_PermissionRegistry.RegisterPermission(m_PluginAccessor.Instance!, $"{KITSKEY}.{kit.Name}");
            }
        }

        public async Task GiveKitAsync(IPlayerUser player, string name)
        {
            m_StringLocalizer ??= m_PluginAccessor.Instance!.LifetimeScope.Resolve<IStringLocalizer>();
            var hasInvertory = (IHasInventory)player.Player;
            if (hasInvertory == null)
            {
                throw new UserFriendlyException(new NotSupportedException("IPlayer doesn't have compatibility IHasInventory").Message);
            }
            var kits = await GetRegisteredKitsAsync();

            var kit = kits.First(c => c.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false);
            if (kit == null)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:notFound", new { Name = name }]);
            }
            if (await m_PermissionChecker.CheckPermissionAsync(player, $"{m_PluginAccessor.Instance!.OpenModComponentId}:{KITSKEY}.{name}")
                != PermissionGrantResult.Grant)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:noPermission", new { Kit = kit }]);
            }
            var balance = await m_EconomyProvider.GetBalanceAsync(player.Id, player.Type);
            if (kit.Cost != 0 && balance < kit.Cost)
            {
                var money = kit.Cost - balance;
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:noMoney",
                    new { Kit = kit, Money = money, MoneyName = m_EconomyProvider.CurrencyName, MoneySymbol = m_EconomyProvider.CurrencySymbol }]);
            }
            if (await m_PermissionChecker.CheckPermissionAsync(player, $"{m_PluginAccessor.Instance.OpenModComponentId}:{Kits.c_NoCooldownPermission}")
                != PermissionGrantResult.Grant)
            {
                /*var kitsCooldown = await m_UserDataStore.GetUserDataAsync<KitsCooldownData>(player.Id, player.Type, COOLDOWNKEY)
                                ?? new KitsCooldownData();*/
                var kitsCooldown = m_KitCooldownCache;
                if (kitsCooldown.KitsCooldown!.TryGetValue(player.Id, out var kitPlayerCooldown))
                {
                    var kitcooldown = kitPlayerCooldown!.Find(x => x.KitName?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false);
                    if (kitcooldown != null)
                    {
                        if ((DateTime.Now - kitcooldown.KitCooldown).TotalSeconds < kit.Cooldown)
                        {
                            var cooldown = Math.Round(kit.Cooldown - (DateTime.Now - kitcooldown.KitCooldown).TotalSeconds);
                            throw new UserFriendlyException(m_StringLocalizer["commands:kit:cooldown", new { Kit = kit, Cooldown = cooldown }]);
                        }
                        else
                        {
                            kitcooldown.KitCooldown = DateTime.Now;
                        }
                    }
                    else
                    {
                        kitcooldown = new KitCooldownData
                        {
                            KitName = name,
                            KitCooldown = DateTime.Now
                        };
                        kitsCooldown.KitsCooldown[player.Id].Add(kitcooldown);
                    }
                }
                else
                {
                    kitsCooldown.KitsCooldown!.Add(player.Id, new List<KitCooldownData>
                    {
                        new KitCooldownData
                        {
                            KitCooldown = DateTime.Now,
                            KitName = name
                        }
                    });
                }
                await DataStore.SaveAsync(COOLDOWNKEY, m_KitCooldownCache);
            }

            if (kit.Cost != 0)
            {
                await m_EconomyProvider.UpdateBalanceAsync(player.Id, player.Type, -kit.Cost,
                    m_StringLocalizer["commans:kit:balanceUpdateReason:buy", new { Kit = kit }]);
            }
            if (kit.Money != 0)
            {
                await m_EconomyProvider.UpdateBalanceAsync(player.Id, player.Type, kit.Money,
                    m_StringLocalizer["commans:kit:balanceUpdateReason:got", new { Kit = kit }]);
            }

            foreach (var item in kit.Items!)
            {
                // https://github.com/openmod/openmod/issues/225 - remove try-catch ArgumentOutOfRangeException when issue will be closed
                try
                {
                    var inventoryItem = await m_ItemSpawner.GiveItemAsync(hasInvertory.Inventory!, item.ItemAssetId, item.State);
                    if (inventoryItem == null)
                    {
                        m_Logger.LogError($"Item {item.ItemAssetId} was unable to give to player {player.DisplayName}({player.Id})");
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // it's dropping item but it throws exception, so we catching it
                }
                catch (Exception e)
                {
                    m_Logger.LogError($"Item {item.ItemAssetId} was unable to give to player {player.DisplayName}({player.Id})", e);
                }
            }

            await player.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);
        }

        public async Task<bool> RemoveKitAsync(string name)
        {
            var kits = await GetRegisteredKitsAsync();
            var removedCount = m_KitCache.Kits!.RemoveAll(c => c.Name!.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removedCount > 0)
            {
                await DataStore.SaveAsync(KITSKEY, m_KitCache);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            m_KitsCooldownWatcher?.Dispose();
            m_KitsWatcher?.Dispose();
        }
    }
}
