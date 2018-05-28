using Musoq.Schema;

namespace Musoq.Service.Core.Windows.Plugins
{
    public class PluginsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = PluginsHelper.PluginColumns;
    }
}