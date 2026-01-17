using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class MultipleSchemasSchemaProvider(IDictionary<string, ISchemaProvider> schemaProviders) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return schemaProviders[schema].GetSchema(schema);
    }
}