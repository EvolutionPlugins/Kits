using Kits.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;
using System;
using System.Threading.Tasks;

[assembly: PluginMetadata("Kits", DisplayName = "Kits", Author = "EvolutionPlugins",
    Website = "https://discord.gg/6KymqGv")]

namespace Kits;

public class KitsPlugin : OpenModUniversalPlugin
{
    private readonly ILogger<KitsPlugin> m_Logger;
    private readonly IServiceProvider m_ServiceProvider;

    public KitsPlugin(ILogger<KitsPlugin> logger, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        m_Logger = logger;
        m_ServiceProvider = serviceProvider;
    }

    protected override Task OnLoadAsync()
    {
        m_Logger.LogInformation("Made with <3 by EvolutionPlugins");
        m_Logger.LogInformation("https://github.com/evolutionplugins \\ https://github.com/diffoz");
        m_Logger.LogInformation("Discord support: https://discord.gg/6KymqGv");

        return m_ServiceProvider.GetRequiredService<IKitStore>().InitAsync();
    }
}