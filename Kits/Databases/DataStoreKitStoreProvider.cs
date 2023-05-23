using Autofac;
using Kits.API.Databases;
using Kits.API.Models;
using Kits.Databases.DataStore;
using OpenMod.API;
using OpenMod.API.Commands;
using OpenMod.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Databases;

public class DataStoreKitStoreProvider : KitStoreProviderCore, IKitStoreProvider, IDisposable
{
    private const string c_KitsKey = "kits";

    private KitsData m_Data = null!;
    private IDisposable? m_FileWatcher;

    public DataStoreKitStoreProvider(ILifetimeScope lifetimeScope) : base(lifetimeScope)
    {
    }

    public async Task InitAsync()
    {
        await LoadFromDisk();

        var component = LifetimeScope.Resolve<IOpenModComponent>();
        m_FileWatcher = DataStore.AddChangeWatcher(c_KitsKey, component,
            () => AsyncHelper.RunSync(LoadFromDisk));
    }

    private async Task LoadFromDisk()
    {
        if (await DataStore.ExistsAsync(c_KitsKey))
        {
            m_Data = await DataStore.LoadAsync<KitsData>(c_KitsKey) ?? new();
            m_Data.Kits ??= new();
            return;
        }

        m_Data = new() { Kits = new() };
        await SaveToDisk();
    }

    public async Task AddKitAsync(Kit kit)
    {
        if (kit is null)
        {
            throw new ArgumentNullException(nameof(kit));
        }

        if (m_Data.Kits.Any(x => x.Name.Equals(kit.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UserFriendlyException(StringLocalizer["commands:kit:exist"]);
        }

        m_Data.Kits?.Add(kit);
        await SaveToDisk();
    }

    public Task<Kit?> FindKitByNameAsync(string name)
    {
        return Task.FromResult(m_Data.Kits?.Find(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IReadOnlyCollection<Kit>> GetKitsAsync()
    {
        return Task.FromResult((IReadOnlyCollection<Kit>)(m_Data.Kits ?? new()));
    }

    public async Task RemoveKitAsync(string name)
    {
        var index = m_Data.Kits?.FindIndex(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new UserFriendlyException(StringLocalizer["commands:kit:remove:fail", new { Name = name }]);
        }

        m_Data.Kits?.RemoveAt(index!.Value);
        await SaveToDisk();
    }

    public async Task UpdateKitAsync(Kit kit)
    {
        if (kit is null)
        {
            throw new ArgumentNullException(nameof(kit));
        }

        var index = m_Data.Kits?.FindIndex(
            x => x.Name.Equals(kit.Name, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return;
        }

        m_Data.Kits![index!.Value] = kit;
        await SaveToDisk();
    }

    public Task<bool> IsKitExists(string name)
    {
        return Task.FromResult(m_Data.Kits?.Find(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) != null);
    }

    private Task SaveToDisk()
    {
        return DataStore.SaveAsync(c_KitsKey, m_Data);
    }

    public void Dispose()
    {
        m_FileWatcher?.Dispose();
        m_FileWatcher = null;
    }
}