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

}
