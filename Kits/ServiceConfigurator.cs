using EvolutionPlugins.Economy.Stub;
using Kits.API.Databases;
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

        serviceCollection.Configure<KitDatabaseOptions>(o =>
        {
            o.AddProvider<DataStoreKitDataStore>("datastore");
            o.AddProvider<MySqlKitDataStore>("mysql");
        });
    }
}