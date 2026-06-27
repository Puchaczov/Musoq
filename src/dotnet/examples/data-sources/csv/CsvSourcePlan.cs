using System.Globalization;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv;

internal static class CsvSourcePlan
{
    public static SourcePlanResult Create(SourcePlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var knownColumns = request.RequiredColumns
            .Select(static column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var predicate = PlanPredicate(request.Predicate, knownColumns);
        var acceptedOrderBy = CanAcceptOrderBy(request.OrderBy, knownColumns)
            ? request.OrderBy
            : [];
        var residualOrderBy = acceptedOrderBy.Count == request.OrderBy.Count
            ? []
            : request.OrderBy;
        var acceptsSlice = predicate.Residual == null && residualOrderBy.Count == 0;
        var acceptedSkip = acceptsSlice ? request.Skip : null;
        var acceptedTake = acceptsSlice ? request.Take : null;

        return new SourcePlanResult
        {
            ExecutionPlan = new SourceExecutionPlan
            {
                Identity = request.Identity,
                AcceptedColumns = request.RequiredColumns,
                AcceptedPredicate = predicate.Accepted,
                AcceptedOrderBy = acceptedOrderBy,
                AcceptedSkip = acceptedSkip,
                AcceptedTake = acceptedTake
            },
            AcceptedColumns = request.RequiredColumns,
            AcceptedPredicate = predicate.Accepted,
            ResidualPredicate = predicate.Residual,
            AcceptedOrderBy = acceptedOrderBy,
            ResidualOrderBy = residualOrderBy,
            AcceptedSkip = acceptedSkip,
            ResidualSkip = acceptsSlice ? null : request.Skip,
            AcceptedTake = acceptedTake,
            ResidualTake = acceptsSlice ? null : request.Take,
            Cardinality = CardinalityEstimate.Unknown("CSV row count depends on file contents and accepted predicates.")
        };
    }

    public static IEnumerable<CsvRow> Apply(
        IEnumerable<CsvRow> rows,
        SourceExecutionPlan plan,
        IReadOnlyList<ISchemaColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(columns);

        var columnsByName = CreateColumnMap(columns);
        var query = rows;

        if (plan.AcceptedPredicate != null)
            query = query.Where(row => EvaluatePredicate(plan.AcceptedPredicate, row, columnsByName));

        if (plan.AcceptedOrderBy.Count > 0)
            query = query.OrderBy(static row => row, new CsvRowComparer(plan.AcceptedOrderBy, columnsByName));

        if (plan.AcceptedSkip.HasValue)
            query = query.Skip(checked((int)plan.AcceptedSkip.Value));

        if (plan.AcceptedTake.HasValue)
            query = query.Take(checked((int)plan.AcceptedTake.Value));

        return query;
    }

    private static PredicatePlan PlanPredicate(
        SourcePredicateExpression? predicate,
        IReadOnlySet<string> knownColumns)
    {
        return predicate switch
        {
            null => PredicatePlan.Empty,
            SourcePredicateComparison comparison => CanEvaluateComparison(comparison, knownColumns)
                ? PredicatePlan.Accept(comparison)
                : PredicatePlan.Reject(comparison),
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical =>
                CombineAnd(
                    PlanPredicate(logical.Left, knownColumns),
                    PlanPredicate(logical.Right, knownColumns)),
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.Or } logical =>
                CanEvaluateWholePredicate(logical, knownColumns)
                    ? PredicatePlan.Accept(logical)
                    : PredicatePlan.Reject(logical),
            SourcePredicateIn inPredicate => CanEvaluateInPredicate(inPredicate, knownColumns)
                ? PredicatePlan.Accept(inPredicate)
                : PredicatePlan.Reject(inPredicate),
            SourcePredicateNullCheck nullCheck => CanEvaluateValue(nullCheck.Expression, knownColumns)
                ? PredicatePlan.Accept(nullCheck)
                : PredicatePlan.Reject(nullCheck),
            _ => PredicatePlan.Reject(predicate)
        };
    }

    private static PredicatePlan CombineAnd(PredicatePlan left, PredicatePlan right)
    {
        return new PredicatePlan(
            CombineLogicalAnd(left.Accepted, right.Accepted),
            CombineLogicalAnd(left.Residual, right.Residual));
    }

    private static SourcePredicateExpression? CombineLogicalAnd(
        SourcePredicateExpression? left,
        SourcePredicateExpression? right)
    {
        if (left == null)
            return right;

        return right == null
            ? left
            : new SourcePredicateLogical(SourcePredicateLogicalOperator.And, left, right);
    }

    private static bool CanEvaluateWholePredicate(
        SourcePredicateExpression predicate,
        IReadOnlySet<string> knownColumns)
    {
        return predicate switch
        {
            SourcePredicateComparison comparison => CanEvaluateComparison(comparison, knownColumns),
            SourcePredicateLogical logical =>
                CanEvaluateWholePredicate(logical.Left, knownColumns) &&
                CanEvaluateWholePredicate(logical.Right, knownColumns),
            SourcePredicateIn inPredicate => CanEvaluateInPredicate(inPredicate, knownColumns),
            SourcePredicateNullCheck nullCheck => CanEvaluateValue(nullCheck.Expression, knownColumns),
            _ => false
        };
    }

    private static bool CanEvaluateComparison(
        SourcePredicateComparison comparison,
        IReadOnlySet<string> knownColumns)
    {
        return CanEvaluateValue(comparison.Left, knownColumns) &&
            CanEvaluateValue(comparison.Right, knownColumns);
    }

    private static bool CanEvaluateInPredicate(
        SourcePredicateIn inPredicate,
        IReadOnlySet<string> knownColumns)
    {
        return CanEvaluateValue(inPredicate.Expression, knownColumns) &&
            inPredicate.Values.All(value => CanEvaluateValue(value, knownColumns));
    }

    private static bool CanEvaluateValue(
        SourcePredicateExpression expression,
        IReadOnlySet<string> knownColumns)
    {
        return expression switch
        {
            SourcePredicateColumn column => knownColumns.Contains(column.Column.Name),
            SourcePredicateLiteral => true,
            _ => false
        };
    }

    private static bool CanAcceptOrderBy(
        IReadOnlyList<OrderByExpression> orderBy,
        IReadOnlySet<string> knownColumns)
    {
        return orderBy.All(order => knownColumns.Contains(order.Column.Name));
    }

    private static bool EvaluatePredicate(
        SourcePredicateExpression predicate,
        CsvRow row,
        IReadOnlyDictionary<string, ISchemaColumn> columnsByName)
    {
        return predicate switch
        {
            SourcePredicateComparison comparison => EvaluateComparison(comparison, row, columnsByName),
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical =>
                EvaluatePredicate(logical.Left, row, columnsByName) &&
                EvaluatePredicate(logical.Right, row, columnsByName),
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.Or } logical =>
                EvaluatePredicate(logical.Left, row, columnsByName) ||
                EvaluatePredicate(logical.Right, row, columnsByName),
            SourcePredicateIn inPredicate => EvaluateIn(inPredicate, row, columnsByName),
            SourcePredicateNullCheck nullCheck =>
                (EvaluateValue(nullCheck.Expression, row, columnsByName) == null) ^ nullCheck.IsNegated,
            _ => throw new InvalidOperationException(
                $"CSV source cannot evaluate predicate '{predicate.GetType().Name}'.")
        };
    }

    private static bool EvaluateComparison(
        SourcePredicateComparison comparison,
        CsvRow row,
        IReadOnlyDictionary<string, ISchemaColumn> columnsByName)
    {
        var left = EvaluateValue(comparison.Left, row, columnsByName);
        var right = EvaluateValue(comparison.Right, row, columnsByName);
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
                $"CSV source cannot evaluate comparison '{comparison.Operator}'.")
        };
    }

    private static bool EvaluateIn(
        SourcePredicateIn inPredicate,
        CsvRow row,
        IReadOnlyDictionary<string, ISchemaColumn> columnsByName)
    {
        var value = EvaluateValue(inPredicate.Expression, row, columnsByName);
        var contains = inPredicate.Values.Any(item => ValuesEqual(
            EvaluateValue(item, row, columnsByName),
            value));
        return inPredicate.IsNegated ? !contains : contains;
    }

    private static object? EvaluateValue(
        SourcePredicateExpression expression,
        CsvRow row,
        IReadOnlyDictionary<string, ISchemaColumn> columnsByName)
    {
        return expression switch
        {
            SourcePredicateColumn column => ReadColumnValue(row, columnsByName, column.Column.Name),
            SourcePredicateLiteral literal => literal.Value,
            _ => throw new InvalidOperationException(
                $"CSV source cannot evaluate value expression '{expression.GetType().Name}'.")
        };
    }

    private static object? ReadColumnValue(
        CsvRow row,
        IReadOnlyDictionary<string, ISchemaColumn> columnsByName,
        string columnName)
    {
        return columnsByName.TryGetValue(columnName, out var column)
            ? row[column.ColumnIndex]
            : throw new InvalidOperationException($"CSV source has no execution column '{columnName}'.");
    }

    private static Dictionary<string, ISchemaColumn> CreateColumnMap(IReadOnlyList<ISchemaColumn> columns)
    {
        var columnsByName = new Dictionary<string, ISchemaColumn>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in columns)
            columnsByName.TryAdd(column.ColumnName, column);

        return columnsByName;
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        if (IsNumber(left) && IsNumber(right))
        {
            return Convert.ToDecimal(left, CultureInfo.InvariantCulture) ==
                Convert.ToDecimal(right, CultureInfo.InvariantCulture);
        }

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

    private sealed record PredicatePlan(
        SourcePredicateExpression? Accepted,
        SourcePredicateExpression? Residual)
    {
        public static PredicatePlan Empty { get; } = new(null, null);

        public static PredicatePlan Accept(SourcePredicateExpression predicate)
        {
            return new PredicatePlan(predicate, null);
        }

        public static PredicatePlan Reject(SourcePredicateExpression predicate)
        {
            return new PredicatePlan(null, predicate);
        }
    }

    private sealed class CsvRowComparer(
        IReadOnlyList<OrderByExpression> orderBy,
        IReadOnlyDictionary<string, ISchemaColumn> columnsByName)
        : IComparer<CsvRow>
    {
        public int Compare(CsvRow? x, CsvRow? y)
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
                    ReadColumnValue(x, columnsByName, order.Column.Name),
                    ReadColumnValue(y, columnsByName, order.Column.Name));

                if (comparison == 0)
                    continue;

                return order.Direction == OrderDirection.Descending
                    ? -comparison
                    : comparison;
            }

            return 0;
        }
    }
}
