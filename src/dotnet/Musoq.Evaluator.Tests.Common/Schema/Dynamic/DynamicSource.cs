using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public interface IDynamicFixtureValues
{
    IReadOnlyDictionary<string, object?> ReadFixtureValues();
}

public class DynamicSource(IEnumerable<dynamic> values) : RowSourceBase<IReadOnlyDictionary<string, object?>>
{
    private readonly IEnumerable<IReadOnlyDictionary<string, object?>> _values = values.Select(ToDictionary).ToArray();

    protected override void CollectChunks(IChunkWriter<IReadOnlyDictionary<string, object?>> writer)
    {
        writer.Write(_values.ToList());
    }

    internal static IReadOnlyDictionary<string, object?> ToDictionary(object value)
    {
        if (value is IDynamicFixtureValues fixture)
            return fixture.ReadFixtureValues();

        if (value is IReadOnlyDictionary<string, object?> readOnlyDictionary)
            return readOnlyDictionary;

        if (value is IDictionary<string, object?> dictionary)
            return dictionary.ToDictionary(pair => pair.Key, pair => pair.Value);

        throw new NotSupportedException("Unregistered dynamic fixture type: " + value.GetType().FullName);
    }
}
