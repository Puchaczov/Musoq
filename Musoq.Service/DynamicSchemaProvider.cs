using System.Linq;
using Musoq.Schema;
using Musoq.Service.Helpers;

namespace Musoq.Service
{
    public class DynamicSchemaProvider : ISchemaProvider
    {
        private readonly ISchema[] _schemas;

        public DynamicSchemaProvider()
        {
            _schemas = PluginsLoader.LoadSchemas();
        }

        public ISchema GetSchema(string schema)
        {
            return _schemas.First(f => $"#{f.Name}" == schema);
        }
    }
}