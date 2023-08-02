using Autofac;
using Kits.API.Databases;
using Kits.API.Models;
using Kits.Databases.MySql;
using Kits.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OpenMod.API.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kits.Databases;

public class MySqlKitStoreProvider : KitStoreProviderCore, IKitStoreProvider, IDisposable
{
    private const string c_CacheKey = "evolutionplugins-kits-kits";
    private readonly IMemoryCache m_MemoryCache;
    private readonly AsyncLock m_AsyncLock = new();

    public MySqlKitStoreProvider(ILifetimeScope lifetimeScope, IMemoryCache memoryCache) : base(lifetimeScope)
    {
        m_MemoryCache = memoryCache;
    }

    protected virtual KitsDbContext GetDbContext() => LifetimeScope.Resolve<KitsDbContext>();

    public async Task InitAsync()
    {
        await using var context = GetDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task AddKitAsync(Kit kit)
    {
        await using var context = GetDbContext();

        if (await context.Kits.AnyAsync(x => x.Name == kit.Name))
        {
            throw new UserFriendlyException(StringLocalizer["commands:kit:exist"]);
        }

        context.Kits.Add(kit);
        await context.SaveChangesAsync();

        if (m_MemoryCache.TryGetValue<List<Kit>>(c_CacheKey, out var kits))
        {
            using var _ = await m_AsyncLock.GetLockAsync();
            kits.Add(kit);
        }
    }

    public async Task<Kit?> FindKitByNameAsync(string name)
    {
        if (TryGetCachedKits(out var kits))
        {
            using var _ = await m_AsyncLock.GetLockAsync();
            return kits.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        await using var context = GetDbContext();

        return await context.Kits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name);
    }

    public async Task<IReadOnlyCollection<Kit>> GetKitsAsync()
    {
        return await GetOrCreatedCachedListOfKitsAsync();
    }

    public async Task RemoveKitAsync(string name)
    {
        await using var context = GetDbContext();

        var kit = await context.Kits
            .FirstOrDefaultAsync(x => x.Name == name);
        if (kit == null)
        {
            return;
        }

        context.Kits.Remove(kit);
        await context.SaveChangesAsync();

        if (!TryGetCachedKits(out var kits))
        {
            return;
        }

        using var _ = await m_AsyncLock.GetLockAsync();

        var kitIndex = kits.FindIndex(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (kitIndex == -1)
        {
            return;
        }

        kits.RemoveAt(kitIndex);
    }

    public async Task UpdateKitAsync(Kit kit)
    {
        await using var context = GetDbContext();

        // start to track the old kit
        var oldKit = await context.Kits.FindAsync(kit.Id);

        // update values
        UpdateValues(oldKit, kit);

        await context.SaveChangesAsync();

        if (!TryGetCachedKits(out var kits))
        {
            return;
        }

        using var _ = await m_AsyncLock.GetLockAsync();
        var newOldKit = kits.Find(x => x.Name.Equals(kit.Name, StringComparison.InvariantCultureIgnoreCase));
        if (newOldKit == null)
        {
            return;
        }

        UpdateValues(newOldKit, kit);

        static void UpdateValues(Kit oldKit, Kit newKit)
        {
            oldKit.Name = newKit.Name;
            oldKit.Cooldown = newKit.Cooldown;
            oldKit.Cost = newKit.Cost;
            oldKit.Money = newKit.Money;
            oldKit.VehicleId = newKit.VehicleId;
            oldKit.Items = newKit.Items;
        }
    }

    public async Task<bool> IsKitExists(string name)
    {
        if (TryGetCachedKits(out var kits))
        {
            using var _ = await m_AsyncLock.GetLockAsync();
            return kits.Exists(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        await using var context = GetDbContext();
        return await context.Kits.AnyAsync(x => x.Name == name);
    }

    private bool TryGetCachedKits(out List<Kit> kits)
    {
        return m_MemoryCache.TryGetValue(c_CacheKey, out kits);
    }

    private async Task<List<Kit>> GetOrCreatedCachedListOfKitsAsync()
    {
        if (!TryGetCachedKits(out var kits))
        {
            using var _ = await m_AsyncLock.GetLockAsync();
            if (TryGetCachedKits(out kits))
            {
                return kits;
            }

            await using var context = GetDbContext();
            kits = await context.Kits.ToListAsync();

            // todo: maybe configure the cache time
            m_MemoryCache.Set(c_CacheKey, kits, TimeSpan.FromHours(1));
        }

        return kits;
    }

    public void Dispose()
    {
        m_AsyncLock.Dispose();
    }
}