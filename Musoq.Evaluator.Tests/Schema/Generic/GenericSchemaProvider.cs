using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericSchemaProvider(IDictionary<string, ISchema> schemas) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return schemas[schema];
    }
}