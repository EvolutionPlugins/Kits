using Kits.API;
using Kits.Databases;
using Kits.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Persistence;
using OpenMod.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Providers
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton)]
    public class KitStore : IKitStore, IDisposable
    {
        public const string c_KitsKey = "kits";

        private readonly Kits m_Plugin;
        private readonly IPermissionRegistry m_PermissionRegistry;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IDataStore m_DataStore;
        private readonly IKitDatabase m_Database;

        private KitsData m_KitsData = null!;
        private IDisposable m_FileWatcher = null!;

        public KitStore(Kits plugin, IPermissionRegistry permissionRegistry, IStringLocalizer stringLocalizer)
        {
            m_Plugin = plugin;
            m_PermissionRegistry = permissionRegistry;
            m_StringLocalizer = stringLocalizer;
            m_DataStore = plugin.DataStore;
            m_Database = plugin.Configuration["database:connectionType"].ToLower() switch
            {
                "mysql" => new MySQLKitDatabase(plugin),
                "datastore" => new DataStoreKitDatabase(plugin),
                _ => throw new Exception()
            };
            AsyncHelper.RunSync(m_Database.LoadDatabaseAsync);
            AsyncHelper.RunSync(RegisterPermissionsAsync);
        }

        public Task<IReadOnlyCollection<Kit>> GetKits()
        {
            return m_Database.GetKitsAsync();
            //await EnsureDataLoaded();
            //return m_KitsData.Kits!;
        }

        public async Task AddKit(Kit kit)
        {
            if (await m_Database.AddKitAsync(kit) && !string.IsNullOrEmpty(kit.Name))
            {
                RegisterPermission(kit.Name!);
            }

            //await EnsureDataLoaded();
            //if (m_KitsData.Kits!.Any(x => x.Name?.Equals(kit.Name, StringComparison.OrdinalIgnoreCase) ?? false))
            //{
            //    throw new UserFriendlyException("Kit with the same name already exists");
            //}
            //m_KitsData.Kits!.Add(kit);
            //await SaveData();
        }

        public async Task<Kit?> GetKit(string kitName)
        {
            var kit = await m_Database.GetKitAsync(kitName);
            if(kit?.Name is not null)
            {
                RegisterPermission(kit.Name);
            }
            return kit;
            //await EnsureDataLoaded();
            //return m_KitsData.Kits!.Find(x => x.Name?.Equals(kitName, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public Task RemoveKit(string kitName)
        {
            return m_Database.RemoveKitAsync(kitName);

            //await EnsureDataLoaded();
            //var index = m_KitsData.Kits!.FindIndex(x => x.Name?.Equals(kitName, StringComparison.OrdinalIgnoreCase) ?? false);
            //if (index < 0)
            //{
            //    throw new UserFriendlyException(m_StringLocalizer["commands:kit:remove:fail", new { Name = kitName }]);
            //}

            //m_KitsData.Kits.RemoveAt(index);
            //await SaveData();
        }

        private async Task EnsureDataLoaded()
        {
            if (m_KitsData == null)
            {
                await LoadData();
                if (m_FileWatcher == null)
                {
                    m_FileWatcher = m_DataStore.AddChangeWatcher(c_KitsKey, m_Plugin, () => AsyncHelper.RunSync(LoadData));
                }
            }
        }

        private async Task RegisterPermissionsAsync()
        {
            foreach (var kit in await m_Database.GetKitsAsync())
            {
                if (kit.Name != null)
                {
                    RegisterPermission(kit.Name);
                }
            }
        }

        private void RegisterPermission(string permission)
        {
            m_PermissionRegistry.RegisterPermission(m_Plugin, permission);
        }

        private async Task LoadData()
        {
            if (await m_DataStore.ExistsAsync(c_KitsKey))
            {
                m_KitsData = await m_DataStore.LoadAsync<KitsData>(c_KitsKey) ?? new() { Kits = new() };
            }
            else
            {
                m_KitsData = new() { Kits = new() };
                await SaveData();
            }

        }

        private Task SaveData()
        {
            return m_KitsData == null ? Task.CompletedTask : m_DataStore.SaveAsync(c_KitsKey, m_KitsData);
        }

        public void Dispose()
        {
            m_FileWatcher?.Dispose();
            (m_Database as IDisposable)?.Dispose();
        }
    }
}
