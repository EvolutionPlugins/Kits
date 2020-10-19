using Autofac;
using Kits.API;
using Kits.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Commands;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Plugins;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Helpers;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IHasInventoryEntity = OpenMod.Extensions.Games.Abstractions.Entities.IHasInventory;

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
        private readonly IUserDataStore m_UserDataStore;

        private IStringLocalizer m_StringLocalizer;
        private IDisposable m_KitsChangeWatcher;
        private KitsData m_KitCache;

        public KitManager(IItemSpawner itemSpawner, IPluginAccessor<Kits> pluginAccessor,
            IPermissionRegistry permissionRegistry, IPermissionChecker permissionChecker, ILogger<KitManager> logger,
            IUserDataStore userDataStore)
        {
            m_ItemSpawner = itemSpawner;
            m_PluginAccessor = pluginAccessor;
            m_PermissionRegistry = permissionRegistry;
            m_PermissionChecker = permissionChecker;
            m_Logger = logger;
            m_UserDataStore = userDataStore;
        }

        public async Task AddKitAsync(Kit kit)
        {
            var kits = await GetRegisteredKitsAsync();
            if (kits.Any(x => x.Name.Equals(kit.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new UserFriendlyException("Kit with the same name already exists");
            }
            m_KitCache.Kits.Add(kit);
            await m_PluginAccessor.Instance.DataStore.SaveAsync(KITSKEY, m_KitCache);
            RegisterPermissions();
        }

        public async Task<IReadOnlyCollection<Kit>> GetRegisteredKitsAsync()
        {
            if (m_KitCache == null)
            {
                await ReadData();
                m_KitsChangeWatcher = m_PluginAccessor.Instance.DataStore.AddChangeWatcher(KITSKEY,
                    m_PluginAccessor.Instance, () => AsyncHelper.RunSync(ReadData));
            }
            return m_KitCache.Kits;
        }

        private async Task ReadData()
        {
            m_KitCache = await m_PluginAccessor.Instance.DataStore.LoadAsync<KitsData>(KITSKEY)
                         ?? new KitsData();
            RegisterPermissions();
        }

        private void RegisterPermissions()
        {
            foreach (var kit in m_KitCache.Kits)
            {
                m_Logger.LogDebug($"Register permission => Kits:{KITSKEY}.{kit.Name}");
                m_PermissionRegistry.RegisterPermission(m_PluginAccessor.Instance, $"{KITSKEY}.{kit.Name}");
            }
        }

        public async Task GiveKitAsync(IPlayerUser player, string name)
        {
            m_StringLocalizer ??= m_PluginAccessor.Instance.LifetimeScope.Resolve<IStringLocalizer>();
            var hasInvertory = (IHasInventoryEntity)player.Player;
            if (hasInvertory == null)
            {
                throw new UserFriendlyException(new NotSupportedException("IPlayer doesn't have compatibility IHasInventory").Message);
            }
            var kits = await GetRegisteredKitsAsync();

            var kit = kits.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (kit == null)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:notFound", new { Name = name }]);
            }
            if (await m_PermissionChecker.CheckPermissionAsync(player, $"{m_PluginAccessor.Instance.OpenModComponentId}:{KITSKEY}.{name}")
                != PermissionGrantResult.Grant)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:noPermission", new { Kit = kit }]);
            }
            var kitsCooldown = await m_UserDataStore.GetUserDataAsync<KitsCooldownData>(player.Id, player.Type, COOLDOWNKEY)
                                ?? new KitsCooldownData();
            if (kitsCooldown.KitsCooldown.TryGetValue(name, out var startCooldown)
                && (DateTime.Now - startCooldown).TotalSeconds < kit.Cooldown)
            {
                var cooldown = Math.Round(kit.Cooldown - (DateTime.Now - startCooldown).TotalSeconds);
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:cooldown", new { Kit = kit, Cooldown = cooldown }]);
            }

            kitsCooldown.KitsCooldown[name] = DateTime.Now;
            await m_UserDataStore.SetUserDataAsync(player.Id, player.Type, COOLDOWNKEY, kitsCooldown);

            foreach (var item in kit.Items)
            {
                var inventoryItem = await m_ItemSpawner.GiveItemAsync(hasInvertory.Inventory, item.ItemAssetId, item.State);
                if (inventoryItem == null)
                {
                    m_Logger.LogWarning($"Item {item.ItemAssetId} was unable to give to player {player.DisplayName}({player.Id})");
                }
            }
            await player.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);
        }

        public async Task<bool> RemoveKitAsync(string name)
        {
            var removedCount = m_KitCache.Kits.RemoveAll(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if(removedCount > 0)
            {
                await m_PluginAccessor.Instance.DataStore.SaveAsync(KITSKEY, m_KitCache);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            m_KitsChangeWatcher?.Dispose();
            // save on dispose?
        }
    }
}
