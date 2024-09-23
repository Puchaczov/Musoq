using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericRowsSource<T>(
    T[] entities,
    IReadOnlyDictionary<string, int> entityNameToIndexMap,
    IReadOnlyDictionary<int, Func<T, object>> entityIndexToObjectAccessMap,
    Func<T, bool> filterEntity = null)
    : RowSourceBase<T>
{
    protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
    {
        chunkedSource.Add(
            entities
                .Where(f => filterEntity?.Invoke(f) ?? true)
                .Select(entity => new EntityResolver<T>(entity, entityNameToIndexMap, entityIndexToObjectAccessMap))
                .ToList()
        );
    }
}