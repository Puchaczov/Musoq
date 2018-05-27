namespace Musoq.Schema.Plugins.Core
{
    public class PluginsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = PluginsHelper.PluginColumns;
    }
}