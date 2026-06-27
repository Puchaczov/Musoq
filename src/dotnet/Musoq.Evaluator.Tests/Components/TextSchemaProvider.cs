using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema provider for text entities.
/// </summary>
public class TextSchemaProvider(IDictionary<string, IEnumerable<TextEntity>> values) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (values.TryGetValue(schema, out var entities)) return new TextSchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
