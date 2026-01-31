using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema provider for binary entities.
/// </summary>
public class BinarySchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<BinaryEntity>> _values;

    public BinarySchemaProvider(IDictionary<string, IEnumerable<BinaryEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        if (_values.TryGetValue(schema, out var entities)) return new BinarySchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
