using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiRowSource<T> : RowSourceBase<T>
{
    private readonly T[] _entities;

    public MultiRowSource(T[] entities)
    {
        _entities = entities;
    }

    protected override void CollectChunks(IChunkWriter<T> writer)
    {
        writer.Write(_entities.ToList());
    }
}
