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
    public class KitManager : IKitManager
    {
        internal const string KITSKEY = "kits";

        private readonly IItemSpawner m_ItemSpawner;
        private readonly IPluginAccessor<Kits> m_PluginAccessor;
        private readonly IPermissionRegistry m_PermissionRegistry;
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly ILogger<KitManager> m_Logger;

        private IStringLocalizer m_StringLocalizer;
        private KitsData m_Cache;

        public KitManager(
            IItemSpawner itemSpawner, IPluginAccessor<Kits> pluginAccessor, IPermissionRegistry permissionRegistry,
            IPermissionChecker permissionChecker, ILogger<KitManager> logger)
        {
            m_ItemSpawner = itemSpawner;
            m_PluginAccessor = pluginAccessor;
            m_PermissionRegistry = permissionRegistry;
            m_PermissionChecker = permissionChecker;
            m_Logger = logger;
        }

        public async Task AddKitAsync(Kit kit)
        {
            var kits = await GetRegisteredKitsAsync();
            if (kits.Any(x => x.Name.Equals(kit.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Kit with the same name already exists");
            }
            m_Cache.Kits.Add(kit);
            await m_PluginAccessor.Instance.DataStore.SaveAsync(KITSKEY, m_Cache);
            RegisterPermissions();
        }

        public async Task<IReadOnlyCollection<Kit>> GetRegisteredKitsAsync()
        {
            if (m_Cache == null)
            {
                await ReadKits();
                RegisterPermissions();
            }
            return m_Cache.Kits;
        }

        private async Task ReadKits()
        {
            m_Cache = await m_PluginAccessor.Instance.DataStore.LoadAsync<KitsData>(KITSKEY) ?? new KitsData();
        }

        private void RegisterPermissions()
        {
            foreach (var kit in m_Cache.Kits)
            {
                m_Logger.LogDebug($"Register permission => Kits:kits.{kit.Name}");
                m_PermissionRegistry.RegisterPermission(m_PluginAccessor.Instance, $"{KITSKEY}.{kit.Name}");
            }
        }
        // todo: check if no cooldown
        public async Task GiveKitAsync(IPlayerUser player, string name)
        {
            m_StringLocalizer ??= m_PluginAccessor.Instance.LifetimeScope.Resolve<IStringLocalizer>();
            var hasInvertory = (IHasInventoryEntity)player.Player;
            if (hasInvertory == null)
            {
                throw new NotSupportedException("IPlayer doesn't have compobillity IHasInventory");
            }
            var kits = await GetRegisteredKitsAsync();
            if (kits == null)
            {
                return;
            }
            var kit = kits.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (kit == null)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:notFound", new { Name = name }]);
            }
            if (await m_PermissionChecker.CheckPermissionAsync(player, $"{m_PluginAccessor.Instance.OpenModComponentId}:{KITSKEY}.{name}") != PermissionGrantResult.Grant)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:noPermission", new { Kit = kit }]);
            }

            foreach (var item in kit.Items)
            {
                await m_ItemSpawner.GiveItemAsync(hasInvertory.Inventory, item.ItemAssetId, item.State);
            }
            await player.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);
        }

        public Task RemoveKitAsync(string name)
        {
            m_Cache.Kits.RemoveAll(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            RegisterPermissions();
            return m_PluginAccessor.Instance.DataStore.SaveAsync(KITSKEY, m_Cache);
        }
    }
}
