using Autofac;
using Kits.API.Databases;
using Kits.API.Models;
using Kits.Databases.MySql;
using Microsoft.EntityFrameworkCore;
using OpenMod.API.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Databases;

public class MySqlKitStoreProvider : KitStoreProviderCore, IKitStoreProvider
{
    public MySqlKitStoreProvider(ILifetimeScope lifetimeScope) : base(lifetimeScope)
    {
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
    }

    public async Task UpdateKitAsync(Kit kit)
    {
        await using var context = GetDbContext();

        context.Kits.Update(kit);
        await context.SaveChangesAsync();
    }

    public async Task<bool> IsKitExists(string name)
    {
        await using var context = GetDbContext();
        return await context.Kits.AnyAsync(x => x.Name == name);
    }
}