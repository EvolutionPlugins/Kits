using Autofac;
using Kits.API;
using Kits.Databases.Mysql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Databases
{
    public class MySqlKitDatabase : KitDatabaseCore, IKitDatabase
    {
        public MySqlKitDatabase(IServiceProvider provider) : this(provider.GetRequiredService<Kits>())
        {
        }

        public MySqlKitDatabase(Kits plugin) : base(plugin)
        {
        }

        protected virtual KitsDbContext GetDbContext() => Plugin.LifetimeScope.Resolve<KitsDbContext>();

        public async Task LoadDatabaseAsync()
        {
            await using var context = GetDbContext();
            await context.Database.MigrateAsync();
        }

        public async Task<bool> AddKitAsync(Kit kit)
        {
            await using var context = GetDbContext();

            if (await context.Kits.Where(x => x.Name.Equals(kit.Name)).AnyAsync())
            {
                throw new UserFriendlyException(StringLocalizer["commands:kit:exist"]);
            }

            await context.Kits.AddAsync(kit);
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<Kit?> FindKitByNameAsync(string name)
        {
            await using var context = GetDbContext();

            return await context.Kits
                .Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyCollection<Kit>> GetKitsAsync()
        {
            await using var context = GetDbContext();
            return await context.Kits.ToListAsync();
        }

        public async Task<bool> RemoveKitAsync(string name)
        {
            await using var context = GetDbContext();

            var kit = await context.Kits
                .Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefaultAsync();
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
}