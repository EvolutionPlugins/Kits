namespace Kits.Databases
{
    internal abstract class KitDatabaseCore
    {
        private readonly Kits m_Plugin;

        protected string TableName => m_Plugin.Configuration["database:connectionTableName"];
        protected string Connection => m_Plugin.Configuration["database:connection"];

        protected KitDatabaseCore(Kits plugin)
        {
            m_Plugin = plugin;
        }
    }
}
