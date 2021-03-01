using Autofac;
using Microsoft.Extensions.Localization;

namespace Kits.Databases
{
    public abstract class KitDatabaseCore
    {
        protected Kits Plugin { get; }
        protected string TableName => Plugin.Configuration["database:connectionTableName"];
        protected string Connection => Plugin.Configuration["database:connection"];
        protected IStringLocalizer StringLocalizer { get; }

        protected KitDatabaseCore(Kits plugin)
        {
            StringLocalizer = plugin.LifetimeScope.Resolve<IStringLocalizer>();
            Plugin = plugin;
        }
    }
}
