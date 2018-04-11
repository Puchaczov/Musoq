using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Service.Exceptions;
using Musoq.Service.Resolvers;

namespace Musoq.Service
{
    public class DynamicSchemaProvider : ISchemaProvider
    {
        private readonly IDictionary<string, Type> _schemas;

        public DynamicSchemaProvider(IDictionary<string, Type> schemas)
        {
            _schemas = schemas;
        }

        public ISchema GetSchema(string schema)
        {
            schema = schema.ToLowerInvariant();

            if (_schemas.ContainsKey(schema))
                return (ISchema) Activator.CreateInstance(_schemas[schema]);
            
            throw new SchemaNotFoundException(schema);
        }
    }
}