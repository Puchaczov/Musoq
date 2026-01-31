using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema provider for text entities.
/// </summary>
public class TextSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<TextEntity>> _values;

    public TextSchemaProvider(IDictionary<string, IEnumerable<TextEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        if (_values.TryGetValue(schema, out var entities)) return new TextSchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
