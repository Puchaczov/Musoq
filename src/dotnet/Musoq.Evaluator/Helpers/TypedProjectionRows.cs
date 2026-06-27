using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Helpers;

public static class TypedProjectionRows
{
    public static IEnumerable<TOut> ProjectValuesSerial<TSource, TOut>(
        IEnumerable<TSource> rows,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        CancellationToken cancellationToken)
    {
        return ProjectionRows.ProjectSerial(rows, predicate, project, cancellationToken);
    }

    public static IEnumerable<TOut> ProjectValuesSerial<TSource, TOut>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var index = 0; index < chunk.Count; index++)
            {
                var row = chunk[index];
                if (predicate(row))
                    yield return project(row);
            }
        }
    }

    public static ValueShard<TOut>[] ProjectValuesParallel<TSource, TOut>(
        IReadOnlyList<TSource> rows,
        int maxDegreeOfParallelism,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        CancellationToken cancellationToken)
    {
        return ProjectionRows.ProjectParallel(
            rows,
            maxDegreeOfParallelism,
            predicate,
            project,
            includeProjected: null,
            PublishValueShard,
            cancellationToken);
    }

    public static IEnumerable<TOut> ProjectChunkedValuesParallel<TSource, TOut>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        int maxDegreeOfParallelism,
        Func<TSource, bool> predicate,
        Func<TSource, TOut> project,
        CancellationToken cancellationToken)
    {
        return ProjectionRows.ProjectChunksParallel(
            chunks,
            maxDegreeOfParallelism,
            predicate,
            project,
            includeProjected: null,
            cancellationToken);
    }

    public static ValueShard<TOut>[] ProjectOptionalValuesParallel<TSource, TOut>(
        IReadOnlyList<TSource> rows,
        int maxDegreeOfParallelism,
        Func<TSource, TOut> project,
        CancellationToken cancellationToken)
        where TOut : class
    {
        return ProjectionRows.ProjectParallel(
            rows,
            maxDegreeOfParallelism,
            static _ => true,
            project,
            static value => value != null,
            PublishValueShard,
            cancellationToken);
    }

    public static IEnumerable<TOut> ProjectChunkedOptionalValuesParallel<TSource, TOut>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        int maxDegreeOfParallelism,
        Func<TSource, TOut> project,
        CancellationToken cancellationToken)
        where TOut : class
    {
        return ProjectionRows.ProjectChunksParallel(
            chunks,
            maxDegreeOfParallelism,
            static _ => true,
            project,
            static value => value != null,
            cancellationToken);
    }

    private static ValueShard<TOut> PublishValueShard<TOut>(TOut[] shard, int count)
    {
        return count == 0
            ? ValueShard<TOut>.Empty
            : new ValueShard<TOut>(shard, count);
    }
}
