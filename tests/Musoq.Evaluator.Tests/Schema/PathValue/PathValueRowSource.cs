using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueRowSource : RowSourceBase<PathValueEntity>
{
    private readonly IEnumerable<PathValueEntity> _entities;

    public PathValueRowSource(IEnumerable<PathValueEntity> entities)
    {
        _entities = entities;
    }

    protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
    {
        var nameToIndexMap = new Dictionary<string, int>
        {
            { "Path", 0 },
            { "Value", 1 }
        };

        var indexToObjectAccessMap = new Dictionary<int, Func<PathValueEntity, object>>
        {
            { 0, entity => entity.Path },
            { 1, entity => entity.Value }
        };

        chunkedSource.Add(
            _entities
                .Select(entity => new EntityResolver<PathValueEntity>(entity, nameToIndexMap, indexToObjectAccessMap))
                .ToList()
        );
    }
}
