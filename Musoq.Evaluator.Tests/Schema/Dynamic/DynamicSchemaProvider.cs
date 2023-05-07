
using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicSchemaProvider : ISchemaProvider
{
    private readonly IReadOnlyDictionary<string, Type> _dynamicSchema;
    private readonly IEnumerable<dynamic> _values;

    public DynamicSchemaProvider(IReadOnlyDictionary<string, Type> dynamicSchema, IEnumerable<dynamic> values)
    {
        _dynamicSchema = dynamicSchema;
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        return new DynamicSchema(_dynamicSchema, _values);
    }
}