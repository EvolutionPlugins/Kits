using Kits.Databases.MySql;
using OpenMod.API.Plugins;
using OpenMod.EntityFrameworkCore.MySql.Extensions;

namespace Kits;

public class PluginContainerConfigurator : IPluginContainerConfigurator
{
    public void ConfigureContainer(IPluginServiceConfigurationContext context)
    {
        context.ContainerBuilder.AddMySqlDbContext<KitsDbContext>();
    }
}
