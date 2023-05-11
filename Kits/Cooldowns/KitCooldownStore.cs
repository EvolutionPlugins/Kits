using Kits.API.Cooldowns;
using Kits.Cooldowns.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Persistence;
using OpenMod.Core.Helpers;
using OpenMod.Core.Permissions;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Threading.Tasks;

[assembly: RegisterPermission("nocooldown", Description = "Allows use kit without waiting for cooldown")]

namespace Kits.Cooldowns;

[PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
public class KitCooldownStore : IKitCooldownStore, IDisposable
{
    private const string c_CooldownKey = "cooldowns";
    private const string c_NoCooldownPermission = "nocooldown";

    private readonly IDataStore m_DataStore;
    private readonly KitsPlugin m_Plugin;
    private readonly IPermissionChecker m_PermissionChecker;

    private KitsCooldownData m_KitsCooldownData = null!;
    private IDisposable? m_FileWatcher;

    public KitCooldownStore(KitsPlugin plugin, IPermissionChecker permissionChecker)
    {
        m_DataStore = plugin.DataStore;
        m_Plugin = plugin;
        m_PermissionChecker = permissionChecker;

        AsyncHelper.RunSync(LoadData);
    }

    public async Task<TimeSpan?> GetLastCooldownAsync(IPlayerUser player, string kitName)
    {
        if (await m_PermissionChecker.CheckPermissionAsync(player, c_NoCooldownPermission) ==
            PermissionGrantResult.Grant
            || !m_KitsCooldownData.KitsCooldown!.TryGetValue(player.Id, out var kitCooldowns))
        {
            return null;
        }

        var kitCooldown = kitCooldowns!.Find(x =>
            x.KitName?.Equals(kitName, StringComparison.CurrentCultureIgnoreCase) == true);
        return kitCooldown == null ? null : DateTime.Now - kitCooldown.KitCooldown;
    }

    public async Task RegisterCooldownAsync(IPlayerUser player, string kitName, DateTime time)
    {
        if (await m_PermissionChecker.CheckPermissionAsync(player, c_NoCooldownPermission) ==
            PermissionGrantResult.Grant)
        {
            return;
        }

        kitName = kitName.ToLower();

        if (m_KitsCooldownData.KitsCooldown!.TryGetValue(player.Id, out var kitCooldowns))
        {
            var kitCooldown = kitCooldowns!.Find(x => x.KitName == kitName);
            if (kitCooldown == null)
            {
                kitCooldown = new() { KitName = kitName };
                kitCooldowns.Add(kitCooldown);
            }

            kitCooldown.KitCooldown = time;
        }
        else
        {
            m_KitsCooldownData.KitsCooldown.Add(player.Id,
                new() { new() { KitCooldown = time, KitName = kitName } });
        }

        await SaveData();
    }

    private async Task LoadFromDisk()
    {
        if (await m_DataStore.ExistsAsync(c_CooldownKey))
        {
            m_KitsCooldownData = await m_DataStore.LoadAsync<KitsCooldownData>(c_CooldownKey) ??
                                 new() { KitsCooldown = new() };
        }
        else
        {
            m_KitsCooldownData = new() { KitsCooldown = new() };
            await SaveData();
        }
    }

    private async Task LoadData()
    {
        await LoadFromDisk();
        m_FileWatcher = m_DataStore.AddChangeWatcher(c_CooldownKey, m_Plugin,
            () => AsyncHelper.RunSync(LoadFromDisk));
    }

    private Task SaveData()
    {
        return m_DataStore.SaveAsync(c_CooldownKey, m_KitsCooldownData);
    }

    public void Dispose()
    {
        m_FileWatcher?.Dispose();
    }
}