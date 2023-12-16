using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class UnknownSchemaProvider : ISchemaProvider
{
    private readonly IEnumerable<dynamic> _values;

    public UnknownSchemaProvider(IEnumerable<dynamic> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        return new UnknownSchema(_values);
    }
}