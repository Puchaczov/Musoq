using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Musoq.Evaluator.Helpers;

internal static class ProjectionRows
{
    private const int CancellationCheckMask = 1023;
    private const int TargetRowsPerWorker = 512;
    private const int ChunkParallelMinimumSourceRows = 4_096;
    private const int ChunkParallelMinimumSourceChunks = 2;
    private const int ChunkParallelMaximumProbeChunks = 64;
    private const int ChunkParallelInFlightMultiplier = 2;

    public static IEnumerable<TOut> ProjectSerial<TSource, TOut>(
        IEnumerable<TSource> rows,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(project);

        var index = 0;
        foreach (var row in rows)
        {
            if (cancellationToken.CanBeCanceled && (index++ & CancellationCheckMask) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            if (predicate(row))
                yield return project(row);
        }
    }

    public static TShard[] ProjectParallel<TSource, TOut, TShard>(
        IReadOnlyList<TSource> rows,
        int maxDegreeOfParallelism,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        Func<TOut[], int, TShard> publishShard,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(publishShard);
        if (rows.Count == 0)
            return [];

        var workerCount = ResolveWorkerCount(rows.Count, maxDegreeOfParallelism);
        var shards = new TShard[workerCount];
        if (workerCount == 1)
        {
            shards[0] = ProjectShard(rows, 0, rows.Count, predicate, project, includeProjected, publishShard, cancellationToken);
            return shards;
        }

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = workerCount
        };

        Parallel.For(0, workerCount, options, shardIndex =>
        {
            var start = rows.Count * shardIndex / workerCount;
            var end = rows.Count * (shardIndex + 1) / workerCount;
            shards[shardIndex] = ProjectShard(rows, start, end, predicate, project, includeProjected, publishShard, cancellationToken);
        });

        return shards;
    }

    public static IEnumerable<TOut> ProjectChunksParallel<TSource, TOut>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        int maxDegreeOfParallelism,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(project);

        var workerCount = Math.Max(1, maxDegreeOfParallelism);
        return workerCount == 1
            ? ProjectChunksSerialCore(chunks, predicate, project, includeProjected, cancellationToken)
            : ProjectChunksParallelCore(
                chunks,
                workerCount,
                predicate,
                project,
                includeProjected,
                cancellationToken);
    }

    private static int ResolveWorkerCount(int rowCount, int maxDegreeOfParallelism)
    {
        var maxWorkers = Math.Min(Math.Max(1, maxDegreeOfParallelism), rowCount);
        var rowLimitedWorkers = Math.Max(
            1,
            (rowCount + TargetRowsPerWorker - 1) / TargetRowsPerWorker);

        return Math.Min(maxWorkers, rowLimitedWorkers);
    }

    private static IEnumerable<TOut> ProjectChunksParallelCore<TSource, TOut>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        int workerCount,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        CancellationToken cancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var chunkEnumerator = chunks.GetEnumerator();

        var bufferedChunks = new Queue<IReadOnlyList<TSource>>(ChunkParallelMaximumProbeChunks);
        var bufferedRows = 0;
        var sourceCompleted = false;
        var shouldUseParallel = false;

        while (true)
        {
            linkedCancellation.Token.ThrowIfCancellationRequested();

            if (!chunkEnumerator.MoveNext())
            {
                sourceCompleted = true;
                break;
            }

            var chunk = chunkEnumerator.Current ?? Array.Empty<TSource>();
            bufferedChunks.Enqueue(chunk);
            bufferedRows = Math.Min(
                ChunkParallelMinimumSourceRows,
                bufferedRows + chunk.Count);

            if (bufferedChunks.Count >= ChunkParallelMinimumSourceChunks &&
                bufferedRows >= ChunkParallelMinimumSourceRows)
            {
                shouldUseParallel = true;
                break;
            }

            if (bufferedChunks.Count >= ChunkParallelMaximumProbeChunks)
                break;
        }

        if (!shouldUseParallel)
        {
            while (bufferedChunks.Count > 0)
            {
                foreach (var projected in ProjectChunkSerial(
                             bufferedChunks.Dequeue(),
                             predicate,
                             project,
                             includeProjected,
                             linkedCancellation.Token))
                {
                    yield return projected;
                }
            }

            while (!sourceCompleted)
            {
                linkedCancellation.Token.ThrowIfCancellationRequested();
                if (!chunkEnumerator.MoveNext())
                    yield break;

                foreach (var projected in ProjectChunkSerial(
                             chunkEnumerator.Current ?? Array.Empty<TSource>(),
                             predicate,
                             project,
                             includeProjected,
                             linkedCancellation.Token))
                {
                    yield return projected;
                }
            }

            yield break;
        }

        var maxInFlight = checked(workerCount * ChunkParallelInFlightMultiplier);
        var inFlight = new Queue<Task<ChunkProjectionResult<TOut>>>(maxInFlight);
        var concurrencyLimiter = new SemaphoreSlim(workerCount, workerCount);
        var disposeConcurrencyLimiter = true;

        try
        {
            while (true)
            {
                linkedCancellation.Token.ThrowIfCancellationRequested();

                while (!sourceCompleted && inFlight.Count < maxInFlight)
                {
                    IReadOnlyList<TSource> chunk;
                    if (bufferedChunks.Count > 0)
                    {
                        chunk = bufferedChunks.Dequeue();
                    }
                    else if (!chunkEnumerator.MoveNext())
                    {
                        sourceCompleted = true;
                        break;
                    }
                    else
                    {
                        chunk = chunkEnumerator.Current ?? Array.Empty<TSource>();
                    }

                    inFlight.Enqueue(StartChunkProjection(
                        chunk,
                        predicate,
                        project,
                        includeProjected,
                        linkedCancellation.Token,
                        concurrencyLimiter));
                }

                if (inFlight.Count == 0)
                    yield break;

                var result = inFlight.Dequeue().GetAwaiter().GetResult();
                for (var index = 0; index < result.Count; index++)
                    yield return result.Values[index];
            }
        }
        finally
        {
            if (!sourceCompleted || inFlight.Count > 0)
            {
                linkedCancellation.Cancel();
                disposeConcurrencyLimiter = false;
                DisposeConcurrencyLimiterWhenTasksComplete(concurrencyLimiter, inFlight.ToArray());
            }

            if (disposeConcurrencyLimiter)
                concurrencyLimiter.Dispose();
        }
    }

    private static IEnumerable<TOut> ProjectChunksSerialCore<TSource, TOut>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        CancellationToken cancellationToken)
    {
        using var chunkEnumerator = chunks.GetEnumerator();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!chunkEnumerator.MoveNext())
                yield break;

            foreach (var projected in ProjectChunkSerial(
                         chunkEnumerator.Current ?? Array.Empty<TSource>(),
                         predicate,
                         project,
                         includeProjected,
                         cancellationToken))
            {
                yield return projected;
            }
        }
    }

    private static Task<ChunkProjectionResult<TOut>> StartChunkProjection<TSource, TOut>(
        IReadOnlyList<TSource>? chunk,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        CancellationToken cancellationToken,
        SemaphoreSlim concurrencyLimiter)
    {
        if (chunk is not { Count: > 0 })
            return Task.FromResult(ChunkProjectionResult<TOut>.Empty);

        return ProjectChunkWithConcurrencyLimit(
            chunk,
            predicate,
            project,
            includeProjected,
            cancellationToken,
            concurrencyLimiter);
    }

    private static async Task<ChunkProjectionResult<TOut>> ProjectChunkWithConcurrencyLimit<TSource, TOut>(
        IReadOnlyList<TSource> chunk,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        CancellationToken cancellationToken,
        SemaphoreSlim concurrencyLimiter)
    {
        await concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Factory.StartNew(
                static state =>
                {
                    var work = (ChunkProjectionWork<TSource, TOut>)state!;
                    return ProjectChunk(
                        work.Chunk,
                        work.Predicate,
                        work.Project,
                        work.IncludeProjected,
                        work.CancellationToken);
                },
                new ChunkProjectionWork<TSource, TOut>(
                    chunk,
                    predicate,
                    project,
                    includeProjected,
                    cancellationToken),
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).ConfigureAwait(false);
        }
        finally
        {
            concurrencyLimiter.Release();
        }
    }

    private static void DisposeConcurrencyLimiterWhenTasksComplete<TOut>(
        SemaphoreSlim concurrencyLimiter,
        IReadOnlyList<Task<ChunkProjectionResult<TOut>>> tasks)
    {
        if (tasks.Count == 0)
        {
            concurrencyLimiter.Dispose();
            return;
        }

        _ = Task.WhenAll(tasks).ContinueWith(
            static (completed, state) =>
            {
                _ = completed.Exception;
                ((SemaphoreSlim)state!).Dispose();
            },
            concurrencyLimiter,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static ChunkProjectionResult<TOut> ProjectChunk<TSource, TOut>(
        IReadOnlyList<TSource> chunk,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        CancellationToken cancellationToken)
    {
        var projectedRows = new TOut[chunk.Count];
        var count = 0;

        for (var index = 0; index < chunk.Count; index++)
        {
            if (cancellationToken.CanBeCanceled && (index & CancellationCheckMask) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            var row = chunk[index];
            if (!predicate(row))
                continue;

            var projected = project(row);
            if (includeProjected == null || includeProjected(projected))
                projectedRows[count++] = projected;
        }

        return count == 0
            ? ChunkProjectionResult<TOut>.Empty
            : new ChunkProjectionResult<TOut>(projectedRows, count);
    }

    private static IEnumerable<TOut> ProjectChunkSerial<TSource, TOut>(
        IReadOnlyList<TSource> chunk,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < chunk.Count; index++)
        {
            if (cancellationToken.CanBeCanceled && (index & CancellationCheckMask) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            var row = chunk[index];
            if (!predicate(row))
                continue;

            var projected = project(row);
            if (includeProjected == null || includeProjected(projected))
                yield return projected;
        }
    }

    private static TShard ProjectShard<TSource, TOut, TShard>(
        IReadOnlyList<TSource> rows,
        int start,
        int end,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        Func<TOut, bool>? includeProjected,
        Func<TOut[], int, TShard> publishShard,
        CancellationToken cancellationToken)
    {
        var shard = new TOut[end - start];
        var count = 0;

        if (!cancellationToken.CanBeCanceled)
        {
            for (var index = start; index < end; index++)
            {
                var row = rows[index];
                if (!predicate(row))
                    continue;

                var projected = project(row);
                if (includeProjected == null || includeProjected(projected))
                    shard[count++] = projected;
            }

            return publishShard(shard, count);
        }

        for (var index = start; index < end; index++)
        {
            if ((index & CancellationCheckMask) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            var row = rows[index];
            if (!predicate(row))
                continue;

            var projected = project(row);
            if (includeProjected == null || includeProjected(projected))
                shard[count++] = projected;
        }

        return publishShard(shard, count);
    }

    private readonly record struct ChunkProjectionWork<TSource, TOut>(
        IReadOnlyList<TSource> Chunk,
        Func<TSource, bool> Predicate,
        Func<TSource, TOut> Project,
        Func<TOut, bool>? IncludeProjected,
        CancellationToken CancellationToken);

    private readonly record struct ChunkProjectionResult<TOut>(TOut[] Values, int Count)
    {
        public static readonly ChunkProjectionResult<TOut> Empty = new(Array.Empty<TOut>(), 0);
    }
}
