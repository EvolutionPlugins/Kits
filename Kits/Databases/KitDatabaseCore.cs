using Autofac;
using Microsoft.Extensions.Localization;

namespace Kits.Databases
{
    public abstract class KitDatabaseCore
    {
        protected Kits Plugin { get; }
        protected virtual string TableName => Plugin.Configuration["database:connectionTableName"];
        protected virtual string Connection => Plugin.Configuration["database:connection"];
        protected IStringLocalizer StringLocalizer { get; }

        protected KitDatabaseCore(Kits plugin)
        {
            StringLocalizer = plugin.LifetimeScope.Resolve<IStringLocalizer>();
            Plugin = plugin;
        }
    }
}
