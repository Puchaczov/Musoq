using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

public class MultipleSchemasSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, ISchemaProvider> _schemaProviders;

    public MultipleSchemasSchemaProvider(IDictionary<string, ISchemaProvider> schemaProviders)
    {
        _schemaProviders = schemaProviders;
    }

    public ISchema GetSchema(string schema)
    {
        return _schemaProviders[schema].GetSchema(schema);
    }
}