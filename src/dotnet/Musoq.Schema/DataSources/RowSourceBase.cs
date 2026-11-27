using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Musoq.Schema.DataSources;

public abstract class RowSourceBase<T> : RowSource<T>
{
    private const int DefaultCapacityInChunks = 4;

    internal TaskScheduler? ProducerScheduler { private get; init; }

    public override IEnumerable<IReadOnlyList<T>> Chunks =>
        new ProducerChunkedEnumerable<T, IChunkWriter<T>>(
            DefaultCapacityInChunks,
            static _ => new CancellationTokenSource(),
            static (chunks, token) => new ChunkWriter<T>(chunks, token),
            CollectChunks,
            producerScheduler: ProducerScheduler
        );

    protected abstract void CollectChunks(IChunkWriter<T> writer);
}
