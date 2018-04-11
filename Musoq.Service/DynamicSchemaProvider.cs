using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Service.Exceptions;
using Musoq.Service.Resolvers;

namespace Musoq.Service
{
    public class DynamicSchemaProvider : ISchemaProvider
    {
        private readonly IDictionary<string, Type> _schemas = CustomDependencyResolver.LoadedSchemas;

        public ISchema GetSchema(string schema)
        {
            schema = schema.ToLowerInvariant();

            if (schema.Contains(schema))
                return (ISchema) Activator.CreateInstance(_schemas[schema]);
            
            throw new SchemaNotFoundException(schema);
        }
    }
}