using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiSchemaProvider(IDictionary<string, ISchema> schemas) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return schemas[schema];
    }
}