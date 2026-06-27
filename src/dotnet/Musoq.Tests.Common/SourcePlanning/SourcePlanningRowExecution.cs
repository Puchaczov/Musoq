using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.Optimization;

namespace Musoq.Tests.Common.SourcePlanning;

public sealed record SourcePlanningRowExecutionOptions<T>(
    string StrategyPropertyName,
    Func<string, Func<T, object?>> CreateKeySelector,
    Func<T, object?> TieBreakerSelector,
    Func<IEnumerable<T>, SourceExecutionPlan, IEnumerable<T>> ApplyProjectionWork);

public enum SourcePlanningExecutionStrategy
{
    NaiveSort,
    TopN,
    NaturalOrder
}

public static class SourcePlanningRowExecution
{
    public static IEnumerable<T> ApplyPlan<T>(
        IEnumerable<T> sourceRows,
        SourceExecutionPlan executionPlan,
        SourcePlanningRowExecutionOptions<T> options)
    {
        ArgumentNullException.ThrowIfNull(sourceRows);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(options);
        var query = ApplyPredicate(
            options.ApplyProjectionWork(sourceRows, executionPlan),
            executionPlan.AcceptedPredicate,
            options.CreateKeySelector);

        query = ApplyOrdering(query, executionPlan, options);

        if (executionPlan.AcceptedSkip.HasValue)
            query = query.Skip((int)executionPlan.AcceptedSkip.Value);

        if (executionPlan.AcceptedTake.HasValue)
            query = query.Take((int)executionPlan.AcceptedTake.Value);

        return query;
    }

    public static IEnumerable<T> ApplyAcceptedColumnWork<T>(
        IEnumerable<T> sourceRows,
        SourceExecutionPlan executionPlan,
        string columnName,
        Action<T> computeColumn)
    {
        ArgumentNullException.ThrowIfNull(sourceRows);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(columnName);
        ArgumentNullException.ThrowIfNull(computeColumn);
        var computesColumn = executionPlan.AcceptedColumns.Count == 0 ||
            executionPlan.AcceptedColumns.Any(column =>
                string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));

        if (!computesColumn)
            return sourceRows;

        return sourceRows.Select(row =>
        {
            computeColumn(row);
            return row;
        });
    }

    private static IEnumerable<T> ApplyPredicate<T>(
        IEnumerable<T> sourceRows,
        SourcePredicateExpression? predicate,
        Func<string, Func<T, object?>> createKeySelector)
    {
        return predicate == null
            ? sourceRows
            : sourceRows.Where(row => EvaluatePredicate(predicate, row, createKeySelector));
    }

    private static bool EvaluatePredicate<T>(
        SourcePredicateExpression predicate,
        T row,
        Func<string, Func<T, object?>> createKeySelector)
    {
        return predicate switch
        {
            SourcePredicateComparison comparison => EvaluateComparison(comparison, row, createKeySelector),
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical =>
                EvaluatePredicate(logical.Left, row, createKeySelector) &&
                EvaluatePredicate(logical.Right, row, createKeySelector),
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.Or } logical =>
                EvaluatePredicate(logical.Left, row, createKeySelector) ||
                EvaluatePredicate(logical.Right, row, createKeySelector),
            SourcePredicateIn inPredicate => EvaluateIn(inPredicate, row, createKeySelector),
            SourcePredicateNullCheck nullCheck =>
                (EvaluateValue(nullCheck.Expression, row, createKeySelector) == null) ^ nullCheck.IsNegated,
            _ => throw new InvalidOperationException($"Unsupported source-planning predicate '{predicate.GetType().Name}'.")
        };
    }

    private static bool EvaluateComparison<T>(
        SourcePredicateComparison comparison,
        T row,
        Func<string, Func<T, object?>> createKeySelector)
    {
        var left = EvaluateValue(comparison.Left, row, createKeySelector);
        var right = EvaluateValue(comparison.Right, row, createKeySelector);
        var valueComparison = CompareValues(left, right);

        return comparison.Operator switch
        {
            SourcePredicateComparisonOperator.Equal => Equals(left, right),
            SourcePredicateComparisonOperator.NotEqual => !Equals(left, right),
            SourcePredicateComparisonOperator.GreaterThan => valueComparison > 0,
            SourcePredicateComparisonOperator.GreaterOrEqual => valueComparison >= 0,
            SourcePredicateComparisonOperator.LessThan => valueComparison < 0,
            SourcePredicateComparisonOperator.LessOrEqual => valueComparison <= 0,
            _ => throw new InvalidOperationException($"Unsupported source-planning comparison '{comparison.Operator}'.")
        };
    }

    private static bool EvaluateIn<T>(
        SourcePredicateIn inPredicate,
        T row,
        Func<string, Func<T, object?>> createKeySelector)
    {
        var value = EvaluateValue(inPredicate.Expression, row, createKeySelector);
        var contains = inPredicate.Values.Any(item => Equals(EvaluateValue(item, row, createKeySelector), value));
        return inPredicate.IsNegated ? !contains : contains;
    }

    private static object? EvaluateValue<T>(
        SourcePredicateExpression expression,
        T row,
        Func<string, Func<T, object?>> createKeySelector)
    {
        return expression switch
        {
            SourcePredicateColumn column => createKeySelector(column.Column.Name)(row),
            SourcePredicateLiteral literal => literal.Value,
            _ => throw new InvalidOperationException($"Unsupported source-planning value expression '{expression.GetType().Name}'.")
        };
    }

    private static IEnumerable<T> ApplyOrdering<T>(
        IEnumerable<T> sourceRows,
        SourceExecutionPlan executionPlan,
        SourcePlanningRowExecutionOptions<T> options)
    {
        if (executionPlan.AcceptedOrderBy.Count == 0)
            return sourceRows;

        return GetStrategy(executionPlan, options.StrategyPropertyName) switch
        {
            SourcePlanningExecutionStrategy.NaturalOrder => sourceRows,
            SourcePlanningExecutionStrategy.TopN when executionPlan.AcceptedTake.HasValue =>
                ApplyTopNOrdering(sourceRows, executionPlan, options),
            _ => sourceRows.OrderBy(static row => row, new SourcePlanningOrderComparer<T>(
                executionPlan.AcceptedOrderBy,
                options.CreateKeySelector,
                options.TieBreakerSelector))
        };
    }

    private static IEnumerable<T> ApplyTopNOrdering<T>(
        IEnumerable<T> sourceRows,
        SourceExecutionPlan executionPlan,
        SourcePlanningRowExecutionOptions<T> options)
    {
        var take = (int)executionPlan.AcceptedTake!.Value;
        var skip = (int)(executionPlan.AcceptedSkip ?? 0);
        var requested = checked(skip + take);

        if (requested <= 0)
            return [];

        var orderComparer = new SourcePlanningOrderComparer<T>(
            executionPlan.AcceptedOrderBy,
            options.CreateKeySelector,
            options.TieBreakerSelector);
        var priorityComparer = new ReverseSourcePlanningOrderComparer<T>(orderComparer);
        var queue = new PriorityQueue<T, T>(priorityComparer);

        foreach (var row in sourceRows)
        {
            if (queue.Count < requested)
            {
                queue.Enqueue(row, row);
                continue;
            }

            var worst = queue.Peek();
            if (orderComparer.Compare(row, worst) >= 0)
                continue;

            queue.Dequeue();
            queue.Enqueue(row, row);
        }

        return queue.UnorderedItems
            .Select(static item => item.Element)
            .OrderBy(static row => row, orderComparer);
    }

    private static SourcePlanningExecutionStrategy GetStrategy(
        SourceExecutionPlan plan,
        string strategyPropertyName)
    {
        if (!plan.Properties.TryGetValue(strategyPropertyName, out var value) || value is not string rawValue)
            return SourcePlanningExecutionStrategy.NaiveSort;

        return Enum.TryParse<SourcePlanningExecutionStrategy>(rawValue, out var strategy)
            ? strategy
            : SourcePlanningExecutionStrategy.NaiveSort;
    }

    private static int CompareValues(object? x, object? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x == null)
            return -1;

        if (y == null)
            return 1;

        if (x is string left && y is string right)
            return string.Compare(left, right, StringComparison.Ordinal);

        return Comparer<object>.Default.Compare(x, y);
    }

    private sealed class SourcePlanningOrderComparer<T>(
        IReadOnlyList<OrderByExpression> orderBy,
        Func<string, Func<T, object?>> createKeySelector,
        Func<T, object?> tieBreakerSelector)
        : IComparer<T>
    {
        public int Compare(T? x, T? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            foreach (var order in orderBy)
            {
                var comparison = CompareValues(
                    createKeySelector(order.Column.Name)(x),
                    createKeySelector(order.Column.Name)(y));

                if (comparison == 0)
                    continue;

                return order.Direction == OrderDirection.Descending
                    ? -comparison
                    : comparison;
            }

            return CompareValues(tieBreakerSelector(x), tieBreakerSelector(y));
        }
    }

    private sealed class ReverseSourcePlanningOrderComparer<T>(IComparer<T> innerComparer)
        : IComparer<T>
    {
        public int Compare(T? x, T? y)
        {
            return -innerComparer.Compare(x, y);
        }
    }
}
