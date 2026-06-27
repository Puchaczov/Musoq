using System.Globalization;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Git;

internal static class GitCommitPlan
{
    public static PredicatePlan PlanPredicate(SourcePredicateExpression? predicate)
    {
        if (predicate == null)
            return PredicatePlan.Empty;

        if (CanEvaluatePredicate(predicate))
            return new PredicatePlan(predicate, null);

        if (predicate is not SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical)
            return new PredicatePlan(null, predicate);

        var left = PlanPredicate(logical.Left);
        var right = PlanPredicate(logical.Right);

        return new PredicatePlan(
            CombineAnd(left.AcceptedPredicate, right.AcceptedPredicate),
            CombineAnd(left.ResidualPredicate, right.ResidualPredicate));
    }

    public static bool CanReadColumns(IEnumerable<SourceColumnRef> columns)
    {
        return columns.All(column => CanReadColumn(column.Name));
    }

    public static bool CanEvaluatePredicate(SourcePredicateExpression? predicate)
    {
        return predicate switch
        {
            null => true,
            SourcePredicateColumn column => CanPushDownColumn(column.Column.Name),
            SourcePredicateLiteral => true,
            SourcePredicateComparison comparison =>
                CanEvaluatePredicate(comparison.Left) &&
                CanEvaluatePredicate(comparison.Right),
            SourcePredicateLogical logical =>
                CanEvaluatePredicate(logical.Left) &&
                CanEvaluatePredicate(logical.Right),
            SourcePredicateIn inPredicate =>
                CanEvaluatePredicate(inPredicate.Expression) &&
                inPredicate.Values.All(CanEvaluatePredicate),
            SourcePredicateNullCheck nullCheck => CanEvaluatePredicate(nullCheck.Expression),
            _ => false
        };
    }

    public static IEnumerable<GitCommitRow> Apply(
        IEnumerable<GitCommitRow> rows,
        SourceExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(plan);

        var query = rows;

        if (plan.AcceptedPredicate != null)
            query = query.Where(row => EvaluatePredicate(plan.AcceptedPredicate, row));

        if (plan.AcceptedOrderBy.Count > 0)
            query = query.OrderBy(static row => row, new GitCommitRowComparer(plan.AcceptedOrderBy));

        if (plan.AcceptedSkip.HasValue)
            query = query.Skip(checked((int)plan.AcceptedSkip.Value));

        if (plan.AcceptedTake.HasValue)
            query = query.Take(checked((int)plan.AcceptedTake.Value));

        return query;
    }

    public static bool CanReadColumn(string name)
    {
        return GitCommitColumnCatalog.TryGetColumn(name, out _);
    }

    public static bool CanPushDownColumn(string name)
    {
        return GitCommitColumnCatalog.TryGetColumn(name, out var column) && !column.IsExpensive;
    }

    public static bool ReferencesExpensiveColumn(SourcePredicateExpression? predicate)
    {
        return predicate switch
        {
            null => false,
            SourcePredicateColumn column => IsExpensiveColumn(column.Column.Name),
            SourcePredicateLiteral => false,
            SourcePredicateComparison comparison =>
                ReferencesExpensiveColumn(comparison.Left) ||
                ReferencesExpensiveColumn(comparison.Right),
            SourcePredicateLogical logical =>
                ReferencesExpensiveColumn(logical.Left) ||
                ReferencesExpensiveColumn(logical.Right),
            SourcePredicateIn inPredicate =>
                ReferencesExpensiveColumn(inPredicate.Expression) ||
                inPredicate.Values.Any(ReferencesExpensiveColumn),
            SourcePredicateNullCheck nullCheck => ReferencesExpensiveColumn(nullCheck.Expression),
            _ => false
        };
    }

    public static bool IsExpensiveColumn(string name)
    {
        return GitCommitColumnCatalog.TryGetColumn(name, out var column) && column.IsExpensive;
    }

    private static SourcePredicateExpression? CombineAnd(
        SourcePredicateExpression? left,
        SourcePredicateExpression? right)
    {
        return (left, right) switch
        {
            (null, null) => null,
            (not null, null) => left,
            (null, not null) => right,
            _ => new SourcePredicateLogical(SourcePredicateLogicalOperator.And, left, right)
        };
    }

    private static bool EvaluatePredicate(SourcePredicateExpression predicate, GitCommitRow row)
    {
        return predicate switch
        {
            SourcePredicateComparison comparison => EvaluateComparison(comparison, row),
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical =>
                EvaluatePredicate(logical.Left, row) && EvaluatePredicate(logical.Right, row),
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.Or } logical =>
                EvaluatePredicate(logical.Left, row) || EvaluatePredicate(logical.Right, row),
            SourcePredicateIn inPredicate => EvaluateIn(inPredicate, row),
            SourcePredicateNullCheck nullCheck =>
                (EvaluateValue(nullCheck.Expression, row) == null) ^ nullCheck.IsNegated,
            _ => throw new InvalidOperationException(
                $"Git commit source cannot evaluate predicate '{predicate.GetType().Name}'.")
        };
    }

    private static bool EvaluateComparison(SourcePredicateComparison comparison, GitCommitRow row)
    {
        var left = EvaluateValue(comparison.Left, row);
        var right = EvaluateValue(comparison.Right, row);
        var valueComparison = CompareValues(left, right);

        return comparison.Operator switch
        {
            SourcePredicateComparisonOperator.Equal => ValuesEqual(left, right),
            SourcePredicateComparisonOperator.NotEqual => !ValuesEqual(left, right),
            SourcePredicateComparisonOperator.GreaterThan => valueComparison > 0,
            SourcePredicateComparisonOperator.GreaterOrEqual => valueComparison >= 0,
            SourcePredicateComparisonOperator.LessThan => valueComparison < 0,
            SourcePredicateComparisonOperator.LessOrEqual => valueComparison <= 0,
            _ => throw new InvalidOperationException(
                $"Git commit source cannot evaluate comparison '{comparison.Operator}'.")
        };
    }

    private static bool EvaluateIn(SourcePredicateIn inPredicate, GitCommitRow row)
    {
        var value = EvaluateValue(inPredicate.Expression, row);
        var contains = inPredicate.Values.Any(item => ValuesEqual(EvaluateValue(item, row), value));
        return inPredicate.IsNegated ? !contains : contains;
    }

    private static object? EvaluateValue(SourcePredicateExpression expression, GitCommitRow row)
    {
        return expression switch
        {
            SourcePredicateColumn column => CreateValueSelector(column.Column.Name)(row),
            SourcePredicateLiteral literal => literal.Value,
            _ => throw new InvalidOperationException(
                $"Git commit source cannot evaluate value expression '{expression.GetType().Name}'.")
        };
    }

    private static Func<GitCommitRow, object?> CreateValueSelector(string columnName)
    {
        return TryGetValueSelector(columnName, out var selector)
            ? selector
            : throw new InvalidOperationException($"Git commit source has no column '{columnName}'.");
    }

    private static bool TryGetValueSelector(
        string columnName,
        out Func<GitCommitRow, object?> selector)
    {
        if (GitCommitColumnCatalog.TryGetColumn(columnName, out var column))
        {
            selector = column.ValueSelector;
            return true;
        }

        selector = static _ => null;
        return false;
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        if (IsNumber(left) && IsNumber(right))
            return Convert.ToDecimal(left, CultureInfo.InvariantCulture) ==
                Convert.ToDecimal(right, CultureInfo.InvariantCulture);

        return Equals(left, right);
    }

    private static int CompareValues(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
            return 0;

        if (left == null)
            return -1;

        if (right == null)
            return 1;

        if (left is string leftText && right is string rightText)
            return string.Compare(leftText, rightText, StringComparison.Ordinal);

        if (IsNumber(left) && IsNumber(right))
        {
            var leftNumber = Convert.ToDecimal(left, CultureInfo.InvariantCulture);
            var rightNumber = Convert.ToDecimal(right, CultureInfo.InvariantCulture);
            return leftNumber.CompareTo(rightNumber);
        }

        return Comparer<object>.Default.Compare(left, right);
    }

    private static bool IsNumber(object value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private sealed class GitCommitRowComparer(IReadOnlyList<OrderByExpression> orderBy)
        : IComparer<GitCommitRow>
    {
        public int Compare(GitCommitRow? x, GitCommitRow? y)
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
                    CreateValueSelector(order.Column.Name)(x),
                    CreateValueSelector(order.Column.Name)(y));

                if (comparison == 0)
                    continue;

                return order.Direction == OrderDirection.Descending
                    ? -comparison
                    : comparison;
            }

            return string.Compare(x.Sha, y.Sha, StringComparison.Ordinal);
        }
    }
}

internal sealed record PredicatePlan(
    SourcePredicateExpression? AcceptedPredicate,
    SourcePredicateExpression? ResidualPredicate)
{
    public static PredicatePlan Empty { get; } = new(null, null);
}
