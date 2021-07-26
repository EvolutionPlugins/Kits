using Autofac;
using Kits.API;
using Kits.API.Database;
using Kits.Databases.Mysql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Databases
{
    public class MySqlKitDatabase : IKitDatabaseProvider
    {
        private readonly ILifetimeScope m_LifetimeScope;
        private readonly IStringLocalizer m_StringLocalizer;

        public MySqlKitDatabase(ILifetimeScope lifetimeScope, IStringLocalizer stringLocalizer)
        {
            m_LifetimeScope = lifetimeScope;
            m_StringLocalizer = stringLocalizer;
        }

        public virtual KitsDbContext GetDbContext() => m_LifetimeScope.Resolve<KitsDbContext>();

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
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:exist"]);
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