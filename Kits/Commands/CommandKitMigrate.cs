extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;
using Kits.API;
using Kits.Databases;
using Kits.Databases.Mysql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenMod.API.Persistence;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using System;
using System.Threading.Tasks;
using Command = OpenMod.Core.Commands.Command;

namespace Kits.Commands
{
    [Command("migrate")]
    [CommandActor(typeof(ConsoleActor))]
    [CommandParent(typeof(CommandKit))]
    [UsedImplicitly]
    public class CommandKitMigrate : Command
    {
        private readonly IServiceProvider m_ServiceProvider;
        private readonly IConfiguration m_Configuration;

        public CommandKitMigrate(IServiceProvider serviceProvider, KitsDbContext dbContext, IConfiguration configuration) : base(serviceProvider)
        {
            m_ServiceProvider = serviceProvider;
            m_Configuration = configuration;
        }

        protected override async Task OnExecuteAsync()
        {
            var mysql = new MySqlKitDatabase(m_ServiceProvider);
            await mysql.LoadDatabaseAsync();

            await using var dbContext = mysql.GetDbContext();

            var oldTableName = m_Configuration["database:connectionTableName"];
            var newTableName = dbContext.Model.FindEntityType(typeof(Kit)).GetTableName();

            // maybe has other option to migrate data to other table
            var affected = await dbContext.Database.ExecuteSqlRawAsync($"INSERT INTO `{newTableName}` (Id, Name, Cooldown, Cost, Money, VehicleId, Items) SELECT Id, Name, Cooldown, Cost, Money, VehicleId, Items FROM `{oldTableName}`");

            await PrintAsync($"Successfully migrated {affected} kit(s) to the new table");
        }
    }
}