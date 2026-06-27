using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericChunkSource<T>(
    IReadOnlyList<T> entities,
    IReadOnlyDictionary<string, int> entityNameToIndexMap,
    IReadOnlyDictionary<int, Func<T, object?>> entityIndexToObjectAccessMap,
    Func<T, bool>? filterEntity = null)
    : RowSourceBase<T>
{
    protected override void CollectChunks(IChunkWriter<T> writer)
    {
        _ = entityNameToIndexMap;
        _ = entityIndexToObjectAccessMap;

        if (filterEntity == null)
        {
            writer.Write(entities);
            return;
        }

        writer.Write(entities.Where(filterEntity).ToArray());
    }
}
