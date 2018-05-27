using System.Reflection;

namespace Musoq.Schema.Plugins.Core
{
    public class PluginEntity
    {
        private readonly Assembly _assembly;

        public PluginEntity(Assembly assembly, string schemaName)
        {
            _assembly = assembly;
            SchemaName = schemaName;
        }

        public string FullName => _assembly.FullName;

        public string Location => _assembly.Location;

        public string SchemaName { get; }
    }
}