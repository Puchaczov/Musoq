using Musoq.Schema.DataSources;

namespace Musoq.Benchmarks;

/// <summary>
///     Generic entity source for benchmarks.
/// </summary>
public class BenchmarkEntitySource<T> : RowSource<T>
{
    private readonly IEnumerable<IReadOnlyList<T>> _chunks;

    public BenchmarkEntitySource(
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
