using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema.DataSources;

public abstract class RowSourceBase<T> : RowSource<T>
{
    private const int DefaultCapacityInChunks = 4;

    public override IEnumerable<IReadOnlyList<T>> Chunks =>
        new ProducerChunkedEnumerable<T, IChunkWriter<T>>(
            DefaultCapacityInChunks,
            static _ => new CancellationTokenSource(),
            static (chunks, token) => new ChunkWriter<T>(chunks, token),
            CollectChunks);

    protected abstract void CollectChunks(IChunkWriter<T> writer);
}
