using Autofac;
using Kits.API;
using Kits.Databases.Mysql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Databases
{
    public sealed class MySqlKitDatabase : KitDatabaseCore, IKitDatabase
    {
        private readonly ILogger<MySqlKitDatabase> m_Logger;
        private readonly KitsDbContext m_Context;

        public MySqlKitDatabase(IServiceProvider provider) : base(provider.GetRequiredService<Kits>())
        {
            m_Logger = provider.GetRequiredService<ILogger<MySqlKitDatabase>>();
            m_Context = provider.GetRequiredService<KitsDbContext>();
        }

        public MySqlKitDatabase(Kits plugin, KitsDbContext context) : base(plugin)
        {
            m_Logger = plugin.LifetimeScope.Resolve<ILogger<MySqlKitDatabase>>();
            m_Context = context;
        }

        public Task LoadDatabaseAsync()
        {
            return m_Context.Database.MigrateAsync();
        }

        public async Task<bool> AddKitAsync(Kit kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            await m_Context.Kits.AddAsync(kit);
            await m_Context.SaveChangesAsync();
            return true;
        }

        public async Task<Kit?> FindKitByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            return await m_Context.Kits.Where(x => x.Name == name).FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyCollection<Kit>> GetKitsAsync()
        {
            return await m_Context.Kits.ToListAsync();
        }

        public async Task<bool> RemoveKitAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }

            var fakeKit = new Kit { Name = name };
            m_Context.Kits.Attach(fakeKit);
            m_Context.Kits.Remove(fakeKit);
            await m_Context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateKitAsync(Kit kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            m_Context.Kits.Update(kit);
            await m_Context.SaveChangesAsync();
            return true;
        }
    }
}