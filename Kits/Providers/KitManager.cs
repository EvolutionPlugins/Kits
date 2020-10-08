using Kits.API;
using Kits.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Plugins;
using OpenMod.API.Prioritization;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;
using Serilog;
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
        private readonly IPluginAccessor<KitsPlugin> m_PluginAccessor;
        private readonly IPermissionRegistry m_PermissionRegistry;
        private readonly IPermissionChecker m_PermissionChecker;

        private KitsData m_Cache;

        public KitManager(IItemSpawner itemSpawner, IPluginAccessor<KitsPlugin> pluginAccessor,
            IPermissionRegistry permissionRegistry, IPermissionChecker permissionChecker)
        {
            m_ItemSpawner = itemSpawner;
            m_PluginAccessor = pluginAccessor;
            m_PermissionRegistry = permissionRegistry;
            m_PermissionChecker = permissionChecker;
        }

        public async Task AddKit(Kit kit)
        {
            var kits = await GetKits();
            if (kits.Any(x => x.Name.Equals(kit.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Kit with the same name already exists");
            }
            m_Cache.Kits.Add(kit);
            await m_PluginAccessor.Instance.DataStore.SaveAsync(KITSKEY, m_Cache);
            RegisterPermissions();
        }

        public async Task<IReadOnlyCollection<Kit>> GetKits()
        {
            // ??= not working :(
            if (m_Cache == null)
            {
                await ReadKits();
                RegisterPermissions();
                
                // Not working
                /*m_PluginAccessor.Instance.DataStore.AddChangeWatcher(KITSKEY, m_PluginAccessor.Instance, async () =>
                {
                    m_Cache = null;
                    await ReadKits();
                    RegisterPermissions();
                    Log.Verbose("Change Watcher trigger");
                });*/
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
                Log.Verbose($"Register permission => Kits:kits.{kit.Name}");
                m_PermissionRegistry.RegisterPermission(m_PluginAccessor.Instance, $"{KITSKEY}.{kit.Name}");
            }
        }
        // todo: check if no cooldown
        public async Task GiveKit(IPlayerUser player, string name)
        {
            var hasInvertory = (IHasInventoryEntity)player.Player;
            if (hasInvertory == null)
            {
                throw new NotSupportedException("IPlayer doesn't have compobillity IHasInventory");
            }
            var kits = await GetKits();
            if (kits == null)
            {
                return;
            }

            var kit = kits.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (kit == null)
            {
                return;
            }
            if (await m_PermissionChecker.CheckPermissionAsync(player, $"{m_PluginAccessor.Instance.OpenModComponentId}:{KITSKEY}.{name}") != PermissionGrantResult.Grant)
            {
                return;
            }

            foreach (var item in kit.Items)
            {
                await m_ItemSpawner.GiveItemAsync(hasInvertory.Inventory, item.ItemAssetId, item.State);
            }
        }

        public Task RemoveKit(string name)
        {
            m_Cache.Kits.RemoveAll(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            RegisterPermissions();
            return m_PluginAccessor.Instance.DataStore.SaveAsync(KITSKEY, m_Cache);
        }
    }
}
