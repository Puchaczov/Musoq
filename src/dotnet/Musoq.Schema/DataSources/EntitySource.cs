using System;
using System.Collections.Generic;

namespace Musoq.Schema.DataSources;

public class EntitySource<T> : RowSource<T>
{
    private readonly IEnumerable<IReadOnlyList<T>> _chunks;

    public EntitySource(IEnumerable<IReadOnlyList<T>> chunks, IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object?>> indexToObjectAccessMap)
    {
        _ = nameToIndexMap;
        _ = indexToObjectAccessMap;

        _chunks = RowChunking.NormalizeSourceChunks(chunks ?? throw new ArgumentNullException(nameof(chunks)));
    }

    public override IEnumerable<IReadOnlyList<T>> Chunks => _chunks;
}
