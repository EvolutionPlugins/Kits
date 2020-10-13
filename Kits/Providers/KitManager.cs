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
        private const string COOLDOWNKEY = "cooldown";

        internal const string KITSKEY = "kits";

        private readonly IItemSpawner m_ItemSpawner;
        private readonly IPluginAccessor<Kits> m_PluginAccessor;
        private readonly IPermissionRegistry m_PermissionRegistry;
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly ILogger<KitManager> m_Logger;

        private IStringLocalizer m_StringLocalizer;
        private IDisposable m_KitsChangeWatcher;
        private IDisposable m_CooldownChangeWatcher;
        private KitsData m_KitCache;
        private KitsCooldownData m_CooldownCache;

        public KitManager(IItemSpawner itemSpawner, IPluginAccessor<Kits> pluginAccessor,
            IPermissionRegistry permissionRegistry, IPermissionChecker permissionChecker, ILogger<KitManager> logger)
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
                RegisterPermissions();
                m_KitsChangeWatcher = m_PluginAccessor.Instance.DataStore.AddChangeWatcher(KITSKEY,
                    m_PluginAccessor.Instance, () => AsyncHelper.RunSync(ReadData));
                m_CooldownChangeWatcher = m_PluginAccessor.Instance.DataStore.AddChangeWatcher(COOLDOWNKEY,
                    m_PluginAccessor.Instance, () => AsyncHelper.RunSync(ReadData));
            }
            return m_KitCache.Kits;
        }

        private async Task ReadData()
        {
            m_KitCache = await m_PluginAccessor.Instance.DataStore.LoadAsync<KitsData>(KITSKEY)
                         ?? new KitsData();
            m_CooldownCache = await m_PluginAccessor.Instance.DataStore.LoadAsync<KitsCooldownData>(COOLDOWNKEY)
                              ?? new KitsCooldownData();
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
            if (m_CooldownCache.KitsCooldown.ContainsKey(player.Id)
                && m_CooldownCache.KitsCooldown[player.Id].TryGetValue(name, out var startCooldown)
                && (DateTime.Now - startCooldown).TotalSeconds < kit.Cooldown)
            {
                var cooldown = kit.Cooldown - (DateTime.Now - startCooldown).TotalSeconds;
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:cooldown", new { Kit = kit, Cooldown = cooldown }]);
            }
            if (await m_PermissionChecker.CheckPermissionAsync(player, $"{m_PluginAccessor.Instance.OpenModComponentId}:{KITSKEY}.{name}")
                != PermissionGrantResult.Grant)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:noPermission", new { Kit = kit }]);
            }

            // todo: convert to users data
            if (!m_CooldownCache.KitsCooldown.ContainsKey(player.Id))
            {
                m_CooldownCache.KitsCooldown.Add(player.Id, new Dictionary<string, DateTime> { { kit.Name, DateTime.Now } });
            }
            else
            {
                m_CooldownCache.KitsCooldown[player.Id][kit.Name] = DateTime.Now;
            }
            await m_PluginAccessor.Instance.DataStore.SaveAsync(COOLDOWNKEY, m_CooldownCache);

            foreach (var item in kit.Items)
            {
                await m_ItemSpawner.GiveItemAsync(hasInvertory.Inventory, item.ItemAssetId, item.State);
            }
            await player.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);
        }

        public Task RemoveKitAsync(string name)
        {
            m_KitCache.Kits.RemoveAll(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return m_PluginAccessor.Instance.DataStore.SaveAsync(KITSKEY, m_KitCache);
        }

        public void Dispose()
        {
            m_KitsChangeWatcher?.Dispose();
            m_CooldownChangeWatcher?.Dispose();
        }
    }
}
