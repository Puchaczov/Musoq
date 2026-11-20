using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Helpers;

public static class TableProjectionRows
{
    public static IEnumerable<TRow> ProjectRowsSerial<TSource, TRow>(
        IEnumerable<TSource> rows,
        Func<TSource, bool> predicate,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken
    )
        where TRow : Row
    {
        return ProjectionRows.ProjectSerial(rows, predicate, project, cancellationToken);
    }

    public static IEnumerable<TRow> ProjectRowsSerial<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken
    )
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(project);
        return new ChunkProjectionRows<TSource, TRow>(chunks, null, project, cancellationToken);
    }

    public static IEnumerable<TRow> ProjectRowsSerial<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, bool> predicate,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken
    )
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
        CancellationToken cancellationToken
    )
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
        CancellationToken cancellationToken
    )
        where TRow : Row
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(project);

        return new OptionalChunkProjectionRows<TSource, TRow>(chunks, project, cancellationToken);
    }

    private static void AppendArraySegment<TSource, TRow>(
        Table table,
        ArraySegment<TSource> segment,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken,
        bool skipNullRows
    )
        where TRow : Row
    {
        var rows = new ReadOnlySpan<TSource>(segment.Array, segment.Offset, segment.Count);
        while (!rows.IsEmpty)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(rows.Length, 1024);
            foreach (var source in rows[..count])
            {
                var row = project(source);
                if (!skipNullRows || row != null)
                    table.AddDirect(row!);
            }
            rows = rows[count..];
        }
    }

    private sealed class OptionalChunkProjectionRows<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken
    ) : ITableRowBatchSource<TRow>
        where TRow : Row
    {
        public void AddTo(Table table)
        {
            ArgumentNullException.ThrowIfNull(table);

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ArrayChunkRows.TryGetSegment(chunk, out var segment))
                {
                    AppendArraySegment(table, segment, project, cancellationToken, skipNullRows: true);
                    continue;
                }

                for (var index = 0; index < chunk.Count; index++)
                {
                    if ((index & 1023) == 0)
                        cancellationToken.ThrowIfCancellationRequested();
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
                    if ((index & 1023) == 0)
                        cancellationToken.ThrowIfCancellationRequested();
                    var projected = project(chunk[index]);
                    if (projected != null)
                        yield return projected;
                }
            }
        }
    }

    private sealed class ChunkProjectionRows<TSource, TRow>(
        IEnumerable<IReadOnlyList<TSource>> chunks,
        Func<TSource, bool>? predicate,
        Func<TSource, TRow> project,
        CancellationToken cancellationToken
    ) : ITableRowBatchSource<TRow>
        where TRow : Row
    {
        public void AddTo(Table table)
        {
            ArgumentNullException.ThrowIfNull(table);
            if (predicate == null)
            {
                var stored = chunks is StoredSourceChunks<TSource> lifecycle
                    ? lifecycle.Storage
                    : chunks as IReadOnlyList<TSource>[];
                if (stored != null)
                    ReserveStoredRows(table, stored);
            }

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (predicate == null && ArrayChunkRows.TryGetSegment(chunk, out var segment))
                {
                    AppendArraySegment(table, segment, project, cancellationToken, skipNullRows: false);
                    continue;
                }

                for (var index = 0; index < chunk.Count; index++)
                {
                    var row = chunk[index];
                    if (predicate == null || predicate(row))
                        table.AddDirect(project(row));
                }
            }
        }

        private void ReserveStoredRows(Table table, IReadOnlyList<TSource>[] stored)
        {
            long count = 0;
            for (var index = 0; index < stored.Length; index++)
            {
                if ((index & 1023) == 0)
                    cancellationToken.ThrowIfCancellationRequested();
                switch (stored[index])
                {
                    case TSource[] array:
                        count += array.Length;
                        break;
                    case RowChunk<TSource> slice:
                        count += slice.Count;
                        break;
                    default:
                        return;
                }
            }

            table.EnsureAdditionalDirectRowCapacity(count);
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
                    if (predicate == null || predicate(row))
                        yield return project(row);
                }
            }
        }
    }
}
