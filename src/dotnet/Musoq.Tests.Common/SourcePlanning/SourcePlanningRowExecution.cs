using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
        var candidates = ApplyPredicateApplications(
            sourceRows,
            executionPlan.PredicateApplications,
            SourcePredicateEvaluationPhase.CandidateMetadata,
            options.CreateKeySelector);
        var query = ApplyPredicate(
            options.ApplyProjectionWork(candidates, executionPlan),
            executionPlan.AcceptedPredicate,
            options.CreateKeySelector);

        query = ApplyOrdering(query, executionPlan, options);

        if (executionPlan.AcceptedSkip.HasValue)
            query = query.Skip((int)executionPlan.AcceptedSkip.Value);

        if (executionPlan.AcceptedTake.HasValue)
            query = query.Take((int)executionPlan.AcceptedTake.Value);

        return query;
    }

    public static IEnumerable<T> ApplyPredicateApplications<T>(
        IEnumerable<T> sourceRows,
        IReadOnlyList<SourcePredicateApplication> applications,
        SourcePredicateEvaluationPhase phase,
        Func<string, Func<T, object?>> createKeySelector)
    {
        ArgumentNullException.ThrowIfNull(sourceRows);
        ArgumentNullException.ThrowIfNull(applications);
        ArgumentNullException.ThrowIfNull(createKeySelector);

        var phaseApplications = applications
            .Where(application => application.Phase == phase)
            .Select(application => new PreparedStringMatch<T>(
                application.Predicate,
                createKeySelector(application.Predicate.Column.Name)))
            .ToArray();

        return phaseApplications.Length == 0
            ? sourceRows
            : sourceRows.Where(row => EvaluateStringMatches(phaseApplications, row));
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
            SourcePredicateStringMatch stringMatch => EvaluateStringMatch(stringMatch, row, createKeySelector),
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

    private static bool EvaluateStringMatch<T>(
        SourcePredicateStringMatch stringMatch,
        T row,
        Func<string, Func<T, object?>> createKeySelector)
    {
        var value = createKeySelector(stringMatch.Column.Name)(row) as string;
        return EvaluateStringMatch(stringMatch, value);
    }
    private static bool EvaluateStringMatches<T>(
        IReadOnlyList<PreparedStringMatch<T>> matches,
        T row)
    {
        foreach (var match in matches)
        {
            if (!EvaluateStringMatch(match.Predicate, match.Selector(row) as string))
                return false;
        }

        return true;
    }

    private static bool EvaluateStringMatch(
        SourcePredicateStringMatch stringMatch,
        string? value)
    {
        if (stringMatch.Comparison != SourceStringComparison.LikeIgnoreCase)
            throw new InvalidOperationException($"Unsupported source string comparison '{stringMatch.Comparison}'.");

        var matches = value != null && MatchesLike(value, stringMatch);

        return stringMatch.IsNegated ? !matches : matches;
    }

    private static bool MatchesLike(string value, SourcePredicateStringMatch stringMatch)
    {
        var needle = stringMatch.Needle;
        var ordinalCompatible = IsOrdinalIgnoreCaseLikeCompatible(needle);

        return stringMatch.Kind switch
        {
            SourceStringMatchKind.Exact =>
                value.Length == needle.Length &&
                (ordinalCompatible && string.Equals(value, needle, StringComparison.OrdinalIgnoreCase) ||
                 (!ordinalCompatible || !IsAscii(value.AsSpan())) && MatchesLikeLegacy(value, stringMatch.OriginalPattern)),
            SourceStringMatchKind.Prefix =>
                value.Length >= needle.Length &&
                (ordinalCompatible && value.StartsWith(needle, StringComparison.OrdinalIgnoreCase) ||
                 (!ordinalCompatible || !IsAscii(value.AsSpan(0, needle.Length))) && MatchesLikeLegacy(value, stringMatch.OriginalPattern)),
            SourceStringMatchKind.Suffix =>
                value.Length >= needle.Length &&
                (ordinalCompatible && value.EndsWith(needle, StringComparison.OrdinalIgnoreCase) ||
                 (!ordinalCompatible || !IsAscii(value.AsSpan(value.Length - needle.Length, needle.Length))) &&
                 MatchesLikeLegacy(value, stringMatch.OriginalPattern)),
            SourceStringMatchKind.Contains =>
                value.Length >= needle.Length &&
                (needle.Length == 0 ||
                 ordinalCompatible && value.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                 (!ordinalCompatible || !IsAscii(value.AsSpan())) && MatchesLikeLegacy(value, stringMatch.OriginalPattern)),
            _ => throw new InvalidOperationException($"Unsupported source string-match kind '{stringMatch.Kind}'.")
        };
    }

    private static bool IsAscii(ReadOnlySpan<char> value)
    {
        foreach (var character in value)
        {
            if (character > 0x7f)
                return false;
        }

        return true;
    }

    private static bool IsOrdinalIgnoreCaseLikeCompatible(string literal)
    {
        if (literal.AsSpan().IndexOfAny('I', 'i') < 0)
            return true;

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToLower('I') == 'i' && textInfo.ToUpper('i') == 'I';
    }

    private static bool MatchesLikeLegacy(string value, string pattern)
    {
        var escaped = Regex.Replace(pattern, @"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", static match => @"\" + match.Value);
        var sqlPattern = escaped.Replace("_", ".", StringComparison.Ordinal).Replace("%", ".*", StringComparison.Ordinal);
        return Regex.IsMatch(
            value,
            string.Concat(@"\A", sqlPattern, @"\z"),
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
            TimeSpan.FromSeconds(2));
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

    private readonly record struct PreparedStringMatch<T>(
        SourcePredicateStringMatch Predicate,
        Func<T, object?> Selector);
}
