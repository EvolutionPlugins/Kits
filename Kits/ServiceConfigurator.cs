using EvolutionPlugins.Economy.Stub;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;

namespace Kits
{
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