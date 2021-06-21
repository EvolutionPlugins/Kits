using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kits.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;

[assembly: PluginMetadata("Kits", DisplayName = "Kits", Author = "EvolutionPlugins",
    Website = "https://discord.gg/6KymqGv")]

namespace Kits
{
    [UsedImplicitly]
    public class Kits : OpenModUniversalPlugin
    {
        private readonly ILogger<Kits> m_Logger;
        private readonly IServiceProvider m_ServiceProvider;

        public Kits(ILogger<Kits> logger, IServiceProvider serviceProvider, KitStore kitStore /* just to trigger database loading */) : base(serviceProvider)
        {
            m_Logger = logger;
            m_ServiceProvider = serviceProvider;
        }

        protected override Task OnLoadAsync()
        {
            m_Logger.LogInformation("Made with <3 by EvolutionPlugins");
            m_Logger.LogInformation("https://github.com/evolutionplugins \\ https://github.com/diffoz");
            m_Logger.LogInformation("Discord support: https://discord.gg/6KymqGv");
            return Task.CompletedTask;
        }
    }
}