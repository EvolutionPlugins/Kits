using EvolutionPlugins.Economy.Stub;
using Kits.API.Cooldowns;
using Kits.API.Databases;
using Kits.Cooldowns.Providers;
using Kits.Databases;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;

namespace Kits;

[Priority(Priority = Priority.Lowest)]
public class ServiceConfigurator : IServiceConfigurator
{
    public void ConfigureServices(IOpenModServiceConfigurationContext openModStartupContext,
        IServiceCollection serviceCollection)
    {
        serviceCollection.AddEconomyStub();

        serviceCollection.Configure<KitStoreOptions>(o =>
        {
            o.AddProvider<DataStoreKitStoreProvider>("datastore");
            o.AddProvider<MySqlKitStoreProvider>("mysql");
        });

        serviceCollection.Configure<KitCooldownOptions>(o =>
        {
            o.AddProvider<DataStoreKitCooldownStoreProvider>("datastore");
        });

        serviceCollection.AddMemoryCache();
    }
}