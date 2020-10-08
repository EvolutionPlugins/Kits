using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;
using System;
using System.Threading.Tasks;

[assembly: PluginMetadata("Kits", DisplayName = "Kits", Author = "DiFFoZ",
    Website = "https://github.com/DiFFoZ/Kits")]

namespace Kits
{
    public class Kits : OpenModUniversalPlugin
    {
        private readonly ILogger<Kits> m_Logger;

        public Kits(ILogger<Kits> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Logger = logger;
        }

        protected override Task OnLoadAsync()
        {
            m_Logger.LogInformation("Made with <3 by DiFFoZ");
            m_Logger.LogInformation("https://github.com/evolutionplugins \\ https://github.com/diffoz");
            m_Logger.LogInformation("Discord: DiFFoZ#6745");
            return Task.CompletedTask;
        }
    }
}
