using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema.DataSources;

internal sealed class ChunkWriter<T> : IChunkWriter<T>
{
    private readonly BlockingCollection<IReadOnlyList<T>> _chunks;

    public ChunkWriter(
        BlockingCollection<IReadOnlyList<T>> chunks,
        CancellationToken token)
    {
        _chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
        CancellationToken = token;
    }

    public CancellationToken CancellationToken { get; }

    public void Write(IReadOnlyList<T> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        try
        {
            foreach (var chunk in RowChunking.NormalizeSourceChunk(rows))
                _chunks.Add(chunk, CancellationToken);
        }
        catch (InvalidOperationException) when (CancellationToken.IsCancellationRequested || _chunks.IsAddingCompleted)
        {
            throw new OperationCanceledException(CancellationToken);
        }
    }
}
