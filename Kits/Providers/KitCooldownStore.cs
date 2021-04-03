using Kits.API;
using Kits.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Persistence;
using OpenMod.Core.Helpers;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Threading.Tasks;

namespace Kits.Providers
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class KitCooldownStore : IKitCooldownStore, IDisposable
    {
        public const string c_CooldownKey = "cooldowns";
        public const string c_NoCooldownPermission = "nocooldown";

        private readonly IDataStore m_DataStore;
        private readonly Kits m_Plugin;
        private readonly IPermissionRegistry m_PermissionRegistry;

        private KitsCooldownData m_KitsCooldownData = null!;
        private IDisposable m_FileWatcher = null!;

        public KitCooldownStore(Kits plugin, IPermissionRegistry permissionRegistry)
        {
            m_DataStore = plugin.DataStore;
            m_Plugin = plugin;
            m_PermissionRegistry = permissionRegistry;

            m_PermissionRegistry.RegisterPermission(plugin, c_NoCooldownPermission, "Allows use kit without waiting for cooldown");
        }

        public async Task<TimeSpan?> GetLastCooldown(IPlayerUser player, string kitName)
        {
            await EnsureDataLoaded();
            if (!m_KitsCooldownData.KitsCooldown!.TryGetValue(player.Id, out var kitCooldowns))
            {
                return null;
            }

            var kitCooldown = kitCooldowns.Find(x => x.KitName == kitName);
            if (kitCooldown == null)
            {
                return null;
            }
            return DateTime.Now - kitCooldown.KitCooldown;
        }

        public async Task RegisterCooldown(IPlayerUser player, string kitName, DateTime time)
        {
            await EnsureDataLoaded();
            if (m_KitsCooldownData.KitsCooldown!.TryGetValue(player.Id, out var kitCooldowns))
            {
                var kitCooldown = kitCooldowns.Find(x => x.KitName == kitName)
                    ?? new() { KitName = kitName, KitCooldown = time };
                kitCooldown.KitCooldown = time;
            }
            else
            {
                m_KitsCooldownData.KitsCooldown.Add(player.Id, new()
                    { new() { KitCooldown = time, KitName = kitName } });
            }
            await SaveData();
        }

        private async Task EnsureDataLoaded()
        {
            if (m_KitsCooldownData == null)
            {
                await LoadData();
                m_FileWatcher ??= m_DataStore.AddChangeWatcher(c_CooldownKey, m_Plugin,
                    () => AsyncHelper.RunSync(LoadData));
            }
        }

        private async Task LoadData()
        {
            if (await m_DataStore.ExistsAsync(c_CooldownKey))
            {
                m_KitsCooldownData = await m_DataStore.LoadAsync<KitsCooldownData>(c_CooldownKey) ?? new()
                {
                    KitsCooldown = new()
                };
            }
            else
            {
                m_KitsCooldownData = new() { KitsCooldown = new() };
                await SaveData();
            }
        }

        private Task SaveData()
        {
            return m_KitsCooldownData == null ? Task.CompletedTask : m_DataStore.SaveAsync(c_CooldownKey, m_KitsCooldownData);
        }

        public void Dispose()
        {
            m_FileWatcher?.Dispose();
        }
    }
}
