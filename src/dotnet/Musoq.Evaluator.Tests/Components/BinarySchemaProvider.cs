using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema provider for binary entities.
/// </summary>
public class BinarySchemaProvider(IDictionary<string, IEnumerable<BinaryEntity>> values) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (values.TryGetValue(schema, out var entities)) return new BinarySchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
