using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator;

public abstract class BaseOperations
{
    public Table Union(Table first, Table second, Func<Row, Row, bool> comparer)
    {
        var result = new Table($"{first.Name}Union{second.Name}", first.Columns.ToArray());

        foreach (var row in first) result.Add(row);

        foreach (var row in second)
            if (!result.Contains(row, comparer))
                result.Add(row);

        return result;
    }

    public Table UnionAll(Table first, Table second, Func<Row, Row, bool> comparer = null)
    {
        var result = new Table($"{first.Name}UnionAll{second.Name}", first.Columns.ToArray());

        foreach (var row in first) result.Add(row);

        foreach (var row in second) result.Add(row);

        return result;
    }

    public Table Except(Table first, Table second, Func<Row, Row, bool> comparer)
    {
        var result = new Table($"{first.Name}Except{second.Name}", first.Columns.ToArray());

        foreach (var row in first)
            if (!second.Contains(row, comparer))
                result.Add(row);

        return result;
    }

    public Table Intersect(Table first, Table second, Func<Row, Row, bool> comparer)
    {
        var result = new Table($"{first.Name}Except{second.Name}", first.Columns.ToArray());

        foreach (var row in first)
            if (second.Contains(row, comparer))
                result.Add(row);

        return result;
    }

    public IOrderedEnumerable<Row> OrderBy<T>(Table table, Func<Row, T> selector)
    {
        return table.OrderBy(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<IObjectResolver> OrderBy<T>(RowSource rowSource, Func<IObjectResolver, T> selector)
    {
        return rowSource.Rows.OrderBy(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<IObjectResolver> OrderBy<T>(IOrderedEnumerable<IObjectResolver> rowSource,
        Func<IObjectResolver, T> selector)
    {
        return rowSource.OrderBy(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<Row> OrderByDescending<T>(Table table, Func<Row, T> selector)
    {
        return table.OrderByDescending(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<IObjectResolver> OrderByDescending<T>(RowSource rowSource,
        Func<IObjectResolver, T> selector)
    {
        return rowSource.Rows.OrderByDescending(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<IObjectResolver> OrderByDescending<T>(IOrderedEnumerable<IObjectResolver> rowSource,
        Func<IObjectResolver, T> selector)
    {
        return rowSource.OrderByDescending(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<Row> ThenBy<T>(IOrderedEnumerable<Row> table, Func<Row, T> selector)
    {
        return table.ThenBy(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<IObjectResolver> ThenBy<T>(IOrderedEnumerable<IObjectResolver> rowSource,
        Func<IObjectResolver, T> selector)
    {
        return rowSource.ThenBy(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<Row> ThenByDescending<T>(IOrderedEnumerable<Row> table, Func<Row, T> selector)
    {
        return table.ThenByDescending(selector, OrdinalFallbackComparer<T>.Instance);
    }

    public IOrderedEnumerable<IObjectResolver> ThenByDescending<T>(IOrderedEnumerable<IObjectResolver> rowSource,
        Func<IObjectResolver, T> selector)
    {
        return rowSource.ThenByDescending(selector, OrdinalFallbackComparer<T>.Instance);
    }

    /// <summary>
    ///     A comparer that uses ordinal comparison for strings and falls back to the default comparer for other types.
    ///     This ensures consistent, culture-independent sorting behavior across all environments.
    /// </summary>
    private sealed class OrdinalFallbackComparer<TKey> : IComparer<TKey>
    {
        public static readonly OrdinalFallbackComparer<TKey> Instance = new();

        public int Compare(TKey? x, TKey? y)
        {
            if (x is string sx && y is string sy)
                return string.Compare(sx, sy, StringComparison.Ordinal);

            return Comparer<TKey>.Default.Compare(x, y);
        }
    }
}
