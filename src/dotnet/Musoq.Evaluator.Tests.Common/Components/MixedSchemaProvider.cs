using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema provider supporting both binary and text entities.
/// </summary>
public class MixedSchemaProvider(
    IDictionary<string, IEnumerable<BinaryEntity>> binaryValues,
    IDictionary<string, IEnumerable<TextEntity>> textValues)
    : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (binaryValues.TryGetValue(schema, out var binaryEntities))
            return new BinarySchema(binaryEntities);
        if (textValues.TryGetValue(schema, out var textEntities))
            return new TextSchema(textEntities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
