using System.Collections.Generic;
using System.Threading;
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
