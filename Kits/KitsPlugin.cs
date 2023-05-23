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
    public KitsPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _ = serviceProvider.GetRequiredService<IKitStore>();
    }

    protected override Task OnLoadAsync()
    {
        Logger.LogInformation("Made with <3 by EvolutionPlugins");
        Logger.LogInformation("https://github.com/evolutionplugins \\ https://github.com/diffoz");
        Logger.LogInformation("Discord support: https://discord.gg/6KymqGv");

        return Task.CompletedTask;
    }
}