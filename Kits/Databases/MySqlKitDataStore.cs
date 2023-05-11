using Autofac;
using Kits.API.Databases;
using Kits.API.Models;
using Kits.Databases.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Databases;

public class MySqlKitDataStore : KitDataStoreCore, IKitDatabaseProvider
{
    public MySqlKitDataStore(IServiceProvider provider) : this(provider.GetRequiredService<KitsPlugin>().LifetimeScope)
    {
    }

    public MySqlKitDataStore(ILifetimeScope lifetimeScope) : base(lifetimeScope)
    {
    }

    protected virtual KitsDbContext GetDbContext() => LifetimeScope.Resolve<KitsDbContext>();

    public async Task LoadDatabaseAsync()
    {
        await using var context = GetDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task<bool> AddKitAsync(Kit kit)
    {
        await using var context = GetDbContext();

        if (await context.Kits.AnyAsync(x => x.Name == kit.Name))
        {
            throw new UserFriendlyException(StringLocalizer["commands:kit:exist"]);
        }

        context.Kits.Add(kit);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<Kit?> FindKitByNameAsync(string name)
    {
        await using var context = GetDbContext();

        return await context.Kits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name);
    }

    public async Task<IReadOnlyCollection<Kit>> GetKitsAsync()
    {
        await using var context = GetDbContext();
        return await context.Kits.AsNoTracking().ToListAsync();
    }

    public async Task<bool> RemoveKitAsync(string name)
    {
        await using var context = GetDbContext();

        var kit = await context.Kits
            .FirstOrDefaultAsync(x => x.Name == name);
        if (kit == null)
        {
            return false;
        }

        context.Kits.Remove(kit);

        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateKitAsync(Kit kit)
    {
        await using var context = GetDbContext();

        context.Kits.Update(kit);
        return await context.SaveChangesAsync() > 0;
    }
}