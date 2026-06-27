using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Unknown;

public class UnknownSchemaProvider(IEnumerable<dynamic> values) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new UnknownSchema(values);
    }
}
