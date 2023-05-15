using Kits.API.Cooldowns;
using Kits.Cooldowns.Models;
using OpenMod.API.Permissions;
using OpenMod.API.Persistence;
using OpenMod.Core.Helpers;
using System;
using System.Threading.Tasks;

namespace Kits.Cooldowns.Providers;
public class DataStoreKitCooldownStoreProvider : IKitCooldownStoreProvider, IDisposable
{
    private const string c_CooldownKey = "cooldowns";

    private readonly KitsPlugin m_Plugin;

    private KitsCooldownData m_KitsCooldownData = null!;
    private IDisposable? m_FileWatcher;

    private IDataStore DataStore => m_Plugin.DataStore;

    public DataStoreKitCooldownStoreProvider(KitsPlugin plugin)
    {
        m_Plugin = plugin;
    }

    public Task<TimeSpan?> GetLastCooldownAsync(IPermissionActor actor, string kitName)
    {
        if (!m_KitsCooldownData.KitsCooldown!.TryGetValue(actor.Id, out var kitCooldowns))
        {
            return Task.FromResult<TimeSpan?>(null);
        }

        var kitCooldown = kitCooldowns!.Find(x =>
            x.KitName?.Equals(kitName, StringComparison.CurrentCultureIgnoreCase) == true);
        return Task.FromResult<TimeSpan?>(kitCooldown == null ? null : DateTime.Now - kitCooldown.KitCooldown);
    }

    public async Task RegisterCooldownAsync(IPermissionActor actor, string kitName, DateTime time)
    {
        if (m_KitsCooldownData.KitsCooldown!.TryGetValue(actor.Id, out var kitCooldowns))
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
            m_KitsCooldownData.KitsCooldown.Add(actor.Id,
                new() { new() { KitCooldown = time, KitName = kitName } });
        }
        await SaveData();
    }

    private async Task LoadFromDisk()
    {
        if (await DataStore.ExistsAsync(c_CooldownKey))
        {
            m_KitsCooldownData = await DataStore.LoadAsync<KitsCooldownData>(c_CooldownKey) ??
                                 new() { KitsCooldown = new() };
        }
        else
        {
            m_KitsCooldownData = new() { KitsCooldown = new() };
            await SaveData();
        }
    }

    public async Task InitAsync()
    {
        await LoadFromDisk();
        m_FileWatcher = DataStore.AddChangeWatcher(c_CooldownKey, m_Plugin,
            () => AsyncHelper.RunSync(LoadFromDisk));
    }

    private Task SaveData()
    {
        return DataStore.SaveAsync(c_CooldownKey, m_KitsCooldownData);
    }

    public void Dispose()
    {
        m_FileWatcher?.Dispose();
        m_FileWatcher = null;
    }
}
