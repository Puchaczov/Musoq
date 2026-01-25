using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiRowSource<T> : RowSourceBase<T>
{
    private readonly T[] _entities;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _entityIndexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _entityNameToIndexMap;

    public MultiRowSource(T[] entities, IReadOnlyDictionary<string, int> entityNameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> entityIndexToObjectAccessMap)
    {
        _entities = entities;
        _entityNameToIndexMap = entityNameToIndexMap;
        _entityIndexToObjectAccessMap = entityIndexToObjectAccessMap;
    }

    protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
    {
        chunkedSource.Add(_entities.Select(entity =>
            new EntityResolver<T>(entity, _entityNameToIndexMap, _entityIndexToObjectAccessMap)).ToList());
    }
}
