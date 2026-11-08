using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Generic row source for test entities.
/// </summary>
public class TestEntitySource<T> : RowSource<T>
{
    private readonly IEnumerable<IReadOnlyList<T>> _chunks;

    public TestEntitySource(
        IEnumerable<IReadOnlyList<T>> chunks,
        IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object?>> indexToObjectAccessMap)
    {
        _ = nameToIndexMap;
        _ = indexToObjectAccessMap;

        _chunks = chunks;
    }

    public override IEnumerable<IReadOnlyList<T>> Chunks => _chunks;
}
