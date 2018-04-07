namespace Musoq.Schema.Plugins
{
    public class PluginsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = PluginsHelper.PluginColumns;
    }
}