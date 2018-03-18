using System.Linq;
using Musoq.Schema;
using Musoq.Service.Exceptions;
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
            schema = schema.ToLowerInvariant();
            var foundedSchema = _schemas.FirstOrDefault(f => $"#{f.Name.ToLowerInvariant()}" == schema);

            if (foundedSchema == null)
                throw new SchemaNotFoundException(schema);

            return foundedSchema;
        }
    }
}