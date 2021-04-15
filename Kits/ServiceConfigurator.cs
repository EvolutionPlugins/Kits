extern alias JetBrainsAnnotations;
using System;
using EvolutionPlugins.Economy.Stub;
using JetBrainsAnnotations::JetBrains.Annotations;
using Kits.Logging;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector.Logging;
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

            try
            {
                MySqlConnectorLogManager.Provider = new SerilogLoggerProviderEx();
                Console.WriteLine("sus");
            }
            catch (InvalidOperationException)
            {
                // already set up
            }
        }
    }
}