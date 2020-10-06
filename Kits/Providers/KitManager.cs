using Kits.API;
using Kits.Models;
using Microsoft.Extensions.DependencyInjection;
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
        private const string KITSKEY = "kits";

        private readonly IItemSpawner m_ItemSpawner;
        private readonly IPluginAccessor<KitsPlugin> m_PluginAccessor;
        private readonly IPermissionRegistry m_PermissionRegistry;

        private KitsData m_Cache;

        public KitManager(IItemSpawner itemSpawner, IPluginAccessor<KitsPlugin> pluginAccessor,
            IPermissionRegistry permissionRegistry)
        {
            m_ItemSpawner = itemSpawner;
            m_PluginAccessor = pluginAccessor;
            m_PermissionRegistry = permissionRegistry;
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
        }

        public async Task<IReadOnlyCollection<Kit>> GetKits()
        {
            // ??= not working :(
            if (m_Cache == null)
            {
                await ReadKits();
                RegisterPermissions();
                m_PluginAccessor.Instance.DataStore.AddChangeWatcher(KITSKEY, m_PluginAccessor.Instance, async () =>
                {
                    m_Cache = null;
                    await ReadKits();
                    RegisterPermissions();
                });
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
                m_PermissionRegistry.RegisterPermission(m_PluginAccessor.Instance, $"{KITSKEY}.{kit.Name}");
            }
        }

        public async Task GiveKit(IPlayer user, string name)
        {
            var hasInvertory = (IHasInventoryEntity)user;
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
            foreach (var item in kit.Items)
            {
                await m_ItemSpawner.GiveItemAsync(hasInvertory.Inventory, item.Asset, item.State);
            }
        }

        public Task RemoveKit(string name)
        {
            m_Cache.Kits.RemoveAll(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return m_PluginAccessor.Instance.DataStore.SaveAsync(KITSKEY, m_Cache);
        }
    }
}
