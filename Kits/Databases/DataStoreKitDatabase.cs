extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Kits.API;
using Kits.Models;
using OpenMod.API.Commands;
using OpenMod.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Databases
{
    public class DataStoreKitDatabase : KitDatabaseCore, IKitDatabase, IDisposable
    {
        private const string c_KitsKey = "kits";

        private KitsData m_Data = null!;
        private IDisposable? m_FileWatcher;

        public DataStoreKitDatabase(Kits plugin) : base(plugin)
        {
        }

        public async Task<bool> AddKitAsync(Kit kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            if (m_Data.Kits.Any(x => x.Name?.Equals(kit.Name, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                throw new UserFriendlyException(StringLocalizer["commands:kit:exist"]);
            }

            m_Data.Kits?.Add(kit);
            await SaveToDisk();
            return true;
        }

        public Task<Kit?> GetKitAsync(string name)
        {
            return Task.FromResult(m_Data.Kits?.Find(x => x.Name?.Equals(name) ?? false));
        }

        public Task<IReadOnlyCollection<Kit>> GetKitsAsync()
        {
            return Task.FromResult((IReadOnlyCollection<Kit>)(m_Data.Kits ?? new()));
        }

        public async Task LoadDatabaseAsync()
        {
            await LoadFromDisk();

            m_FileWatcher = Plugin.DataStore.AddChangeWatcher(c_KitsKey, Plugin,
                () => AsyncHelper.RunSync(LoadFromDisk));

            await SaveToDisk();
        }

        private async Task LoadFromDisk()
        {
            if (await Plugin.DataStore.ExistsAsync(c_KitsKey))
            {
                m_Data = await Plugin.DataStore.LoadAsync<KitsData>(c_KitsKey) ?? new() { Kits = new() };
            }
            else
            {
                m_Data = new() { Kits = new() };
            }
        }

        public async Task<bool> RemoveKitAsync(string name)
        {
            var index = m_Data.Kits?.FindIndex(x => x.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false);
            if (index < 0)
            {
                throw new UserFriendlyException(StringLocalizer["commands:kit:remove:fail", new { Name = name }]);
            }

            m_Data.Kits?.RemoveAt(index!.Value);
            await SaveToDisk();
            return true;
        }

        public async Task<bool> UpdateKitAsync(Kit kit)
        {
            if (kit is null)
            {
                throw new ArgumentNullException(nameof(kit));
            }

            var index = m_Data.Kits?.FindIndex(
                x => x.Name?.Equals(kit.Name, StringComparison.OrdinalIgnoreCase) ?? false);
            if (index < 0)
            {
                return false;
            }

            m_Data.Kits![index!.Value] = kit;
            await SaveToDisk();
            return true;
        }

        public void Dispose()
        {
            m_FileWatcher?.Dispose();
        }

        private Task SaveToDisk()
        {
            return Plugin.DataStore.SaveAsync(c_KitsKey, m_Data);
        }
    }
}