using System;
using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema provider supporting both binary and text entities.
/// </summary>
public class MixedSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<BinaryEntity>> _binaryValues;
    private readonly IDictionary<string, IEnumerable<TextEntity>> _textValues;

    public MixedSchemaProvider(
        IDictionary<string, IEnumerable<BinaryEntity>> binaryValues,
        IDictionary<string, IEnumerable<TextEntity>> textValues)
    {
        _binaryValues = binaryValues;
        _textValues = textValues;
    }

    public ISchema GetSchema(string schema)
    {
        if (_binaryValues.TryGetValue(schema, out var binaryEntities))
            return new BinarySchema(binaryEntities);
        if (_textValues.TryGetValue(schema, out var textEntities))
            return new TextSchema(textEntities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}
