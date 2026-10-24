using System.Collections.Generic;
using System.Threading;
using Musoq.Plugins;

namespace Musoq.Evaluator.Runtime;

public static class SubqueryExecution
{
    public static IEnumerable<Musoq.Evaluator.Tables.Row> Rows(Musoq.Evaluator.Tables.Table table)
    {
        using (table)
            foreach (var row in table.Rows)
                yield return row;
    }

    public static TValue Scalar<TRow, TValue>(IEnumerable<TRow> rows, Func<TRow, TValue> read, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(read);
        token.ThrowIfCancellationRequested();
        using var iterator = rows.GetEnumerator();
        if (!iterator.MoveNext())
            return default!;
        token.ThrowIfCancellationRequested();
        var value = read(iterator.Current);
        if (iterator.MoveNext())
            return CorrelatedScalarSubqueryResultExtractor.GetValue(new CorrelatedScalarSubqueryResult<TValue>(default!, 2));
        token.ThrowIfCancellationRequested();
        return value;
    }

    public static bool Exists<TRow>(IEnumerable<TRow> rows, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(rows);
        token.ThrowIfCancellationRequested();
        using var iterator = rows.GetEnumerator();
        var exists = iterator.MoveNext();
        token.ThrowIfCancellationRequested();
        return exists;
    }

    public static bool? In<TRow, TLeft, TValue>(TLeft left, IEnumerable<TRow> rows, Func<TRow, TValue> read,
        Func<TLeft, TValue, bool?> compare, CancellationToken token) => Any(left, rows, read, compare, token);

    public static bool? Any<TRow, TLeft, TValue>(TLeft left, IEnumerable<TRow> rows, Func<TRow, TValue> read,
        Func<TLeft, TValue, bool?> compare, CancellationToken token) =>
        Reduce(left, rows, read, compare, token, QuantifiedReduction.Any);

    public static bool? All<TRow, TLeft, TValue>(TLeft left, IEnumerable<TRow> rows, Func<TRow, TValue> read,
        Func<TLeft, TValue, bool?> compare, CancellationToken token) =>
        Reduce(left, rows, read, compare, token, QuantifiedReduction.All);

    private static bool? Reduce<TRow, TLeft, TValue>(TLeft left, IEnumerable<TRow> rows, Func<TRow, TValue> read,
        Func<TLeft, TValue, bool?> compare, CancellationToken token, QuantifiedReduction reduction)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(compare);
        token.ThrowIfCancellationRequested();
        var unknown = false;
        var decisive = reduction == QuantifiedReduction.Any;
        foreach (var row in rows)
        {
            token.ThrowIfCancellationRequested();
            var comparison = compare(left, read(row));
            token.ThrowIfCancellationRequested();
            if (comparison == decisive)
                return decisive;
            unknown |= comparison is null;
        }
        token.ThrowIfCancellationRequested();
        return unknown ? null : !decisive;
    }

    private enum QuantifiedReduction { Any, All }
}
