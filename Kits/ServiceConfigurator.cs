extern alias JetBrainsAnnotations;
using EvolutionPlugins.Economy.Stub;
using JetBrainsAnnotations::JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;

namespace Kits
{
    [UsedImplicitly]
    [Priority(Priority = Priority.Lowest)]
    public class ServiceConfigurator : IServiceConfigurator
    {
        public void ConfigureServices(IOpenModServiceConfigurationContext openModStartupContext,
            IServiceCollection serviceCollection)
        {
            serviceCollection.AddEconomyStub();
        }
    }
}