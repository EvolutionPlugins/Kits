using Autofac;
using JetBrains.Annotations;
using Kits.API;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;
using System;
using System.Threading.Tasks;

[assembly: PluginMetadata("Kits", DisplayName = "Kits", Author = "EvolutionPlugins",
    Website = "https://discord.gg/6KymqGv")]

namespace Kits
{
    [UsedImplicitly]
    public class Kits : OpenModUniversalPlugin
    {
        private readonly ILogger<Kits> m_Logger;
        private readonly IKitDatabaseManager m_KitDatabase;
        private readonly ILifetimeScope m_LifetimeScope;

        public Kits(ILogger<Kits> logger, IServiceProvider serviceProvider, IKitDatabaseManager kitDatabase, ILifetimeScope lifetimeScope)
            : base(serviceProvider)
        {
            m_Logger = logger;
            m_KitDatabase = kitDatabase;
            m_LifetimeScope = lifetimeScope;
        }

        protected override Task OnLoadAsync()
        {
            m_Logger.LogInformation("Made with <3 by EvolutionPlugins");
            m_Logger.LogInformation("https://github.com/evolutionplugins \\ https://github.com/diffoz");
            m_Logger.LogInformation("Discord support: https://discord.gg/6KymqGv");

            return m_KitDatabase.InitAsync(m_LifetimeScope);
        }
    }
}