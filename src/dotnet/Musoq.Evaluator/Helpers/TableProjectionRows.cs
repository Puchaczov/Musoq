using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static class TableProjectionRows
{
    public static IEnumerable<TRow> ProjectRowsSerial<TSource, TRow>(
        IEnumerable<TSource> rows,
        Func<TSource, bool> predicate,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        return ProjectionRows.ProjectSerial(rows, predicate, project, cancellationToken);
    }

    public static IEnumerable<TRow> ProjectRowsSerial<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, bool> predicate,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(project);

        return new ChunkProjectionRows<TSource, TRow>(chunks, predicate, project, cancellationToken);
    }

    public static IEnumerable<TRow> ProjectOptionalRowsSerial<TSource, TRow>(
        IEnumerable<TSource> rows,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(project);

        var index = 0;
        foreach (var row in rows)
        {
            if (cancellationToken.CanBeCanceled && (index++ & 1023) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            var projected = project(row);
            if (projected != null)
                yield return projected;
        }
    }

    public static IEnumerable<TRow> ProjectOptionalRowsSerial<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken)
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(project);

        return new OptionalChunkProjectionRows<TSource, TRow>(chunks, project, cancellationToken);
    }

    private sealed class OptionalChunkProjectionRows<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken) : ITableRowBatchSource<TRow>
        where TRow : Row
    {
        public void AddTo(Table table)
        {
            ArgumentNullException.ThrowIfNull(table);

            var capacity = table.Count;
            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // A chunk is an upper bound for the number of projected rows. Reserving it here
                // avoids List<Row>'s geometric growth without creating an intermediate result list.
                capacity += chunk.Count;
                table.EnsureCapacity(capacity);

                for (var index = 0; index < chunk.Count; index++)
                {
                    var projected = project(chunk[index]);
                    if (projected != null)
                        table.AddDirect(projected);
                }
            }
        }

        public IEnumerator<TRow> GetEnumerator()
        {
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<TRow> Enumerate()
        {
            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var index = 0; index < chunk.Count; index++)
                {
                    var projected = project(chunk[index]);
                    if (projected != null)
                        yield return projected;
                }
            }
        }
    }

    private sealed class ChunkProjectionRows<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, bool> predicate,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken) : ITableRowBatchSource<TRow>
        where TRow : Row
    {
        public void AddTo(Table table)
        {
            ArgumentNullException.ThrowIfNull(table);

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var index = 0; index < chunk.Count; index++)
                {
                    var row = chunk[index];
                    if (predicate(row))
                        table.AddDirect(project(row));
                }
            }
        }

        public IEnumerator<TRow> GetEnumerator()
        {
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<TRow> Enumerate()
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
    }
}
