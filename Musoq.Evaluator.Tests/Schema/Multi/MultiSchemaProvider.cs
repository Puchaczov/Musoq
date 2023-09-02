using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, ISchema> _schemas;

    public MultiSchemaProvider(IDictionary<string, ISchema> schemas)
    {
        _schemas = schemas;
    }

    public ISchema GetSchema(string schema)
    {
        return _schemas[schema];
    }
}