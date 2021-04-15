extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Kits.Databases;
using Kits.Models;
using OpenMod.API.Commands;
using OpenMod.API.Persistence;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using System;
using System.Threading.Tasks;

namespace Kits.Commands
{
    [Command("migrate")]
    [CommandActor(typeof(ConsoleActor))]
    [CommandParent(typeof(CommandKit))]
    [UsedImplicitly]
    public class CommandKitMigrate : Command
    {
        private readonly IDataStore m_DataStore;
        private readonly Kits m_Plugin;

        public CommandKitMigrate(IServiceProvider serviceProvider, IDataStore dataStore, Kits plugin) : base(serviceProvider)
        {
            m_DataStore = dataStore;
            m_Plugin = plugin;
        }

        protected override async Task OnExecuteAsync()
        {
            if (!await m_DataStore.ExistsAsync("kits"))
            {
                throw new UserFriendlyException("Unable to find 'kits.data.yaml'");
            }

            var kits = await m_DataStore.LoadAsync<KitsData>("kits");
            if (kits?.Kits == null)
            {
                throw new UserFriendlyException("Unable to load 'kits.data.yaml'");
            }

            var mysql = new MySqlKitDatabase(m_Plugin);
            await mysql.LoadDatabaseAsync();
            
            foreach (var kit in kits.Kits)
            {
                await mysql.AddKitAsync(kit);
            }

            await PrintAsync("All kits data from 'kits.data.yaml' successfully migrated to MySQL");
            await PrintAsync("Please set 'connectionType' to \"mysql\" in 'config.yaml' to enable MySQL database");
        }
    }
}