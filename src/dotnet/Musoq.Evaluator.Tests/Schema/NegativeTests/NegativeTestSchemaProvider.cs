using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class NegativeTestSchemaProvider(IDictionary<string, ISchema> schemas) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (schemas.TryGetValue(schema, out var found))
            return found;

        throw new SourceNotFoundException($"Schema '{schema}' not found.");
    }
}
