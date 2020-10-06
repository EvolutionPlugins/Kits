using Kits.API;
using Kits.Providers;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;

namespace Kits
{
    public class ServiceConfigurator : IServiceConfigurator
    {
        public void ConfigureServices(IOpenModServiceConfigurationContext openModStartupContext, IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IKitManager, KitManager>();
        }
    }
}
