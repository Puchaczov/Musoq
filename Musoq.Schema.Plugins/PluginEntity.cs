using System.Reflection;

namespace Musoq.Schema.Plugins
{
    public class PluginEntity
    {
        private readonly Assembly _assembly;
        private readonly string _schemaName;

        public PluginEntity(Assembly assembly, string schemaName)
        {
            _assembly = assembly;
            _schemaName = schemaName;
        }

        public string FullName => _assembly.FullName;

        public string Location => _assembly.Location;

        public string SchemaName => _schemaName;
    }
}