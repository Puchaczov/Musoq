using System.Collections.Generic;
using Musoq.Schema.Optimization;

namespace Musoq.Tests.Common.SourcePlanning;

public static class SourcePlanningPlanResultBuilder
{
    public static SourcePlanResult CreateAccepted(
        SourcePlanRequest request,
        IReadOnlyList<OrderByExpression> acceptedOrderBy,
        IReadOnlyList<OrderByExpression> residualOrderBy,
        long? acceptedSkip,
        long? residualSkip,
        long? acceptedTake,
        long? residualTake,
        IReadOnlyList<SourceColumnRef>? acceptedColumns = null,
        SourcePredicateExpression? acceptedPredicate = null,
        SourcePredicateExpression? residualPredicate = null,
        bool predicateAccepted = false,
        string? strategyPropertyName = null,
        SourcePlanningExecutionStrategy? strategy = null,
        string? projectionWorkPropertyName = null,
        bool projectionWork = false)
    {
        ArgumentNullException.ThrowIfNull(request);
        var properties = CreateProperties(
            strategyPropertyName,
            strategy,
            projectionWorkPropertyName,
            projectionWork);
        var planAcceptedColumns = acceptedColumns ?? request.RequiredColumns;
        var planAcceptedPredicate = predicateAccepted
            ? acceptedPredicate ?? request.Predicate
            : acceptedPredicate;

        return new SourcePlanResult
        {
            ExecutionPlan = new SourceExecutionPlan
            {
                Identity = request.Identity,
                AcceptedColumns = planAcceptedColumns,
                AcceptedPredicate = planAcceptedPredicate,
                AcceptedOrderBy = acceptedOrderBy,
                AcceptedSkip = acceptedSkip,
                AcceptedTake = acceptedTake,
                Properties = properties
            },
            AcceptedColumns = planAcceptedColumns,
            AcceptedPredicate = planAcceptedPredicate,
            ResidualPredicate = predicateAccepted ? residualPredicate : residualPredicate ?? request.Predicate,
            AcceptedOrderBy = acceptedOrderBy,
            ResidualOrderBy = residualOrderBy,
            AcceptedSkip = acceptedSkip,
            ResidualSkip = residualSkip,
            AcceptedTake = acceptedTake,
            ResidualTake = residualTake
        };
    }

    public static SourcePlanResult CreateRejectedWithProperties(
        SourcePlanRequest request,
        string? projectionWorkPropertyName = null,
        bool projectionWork = false)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rejected = SourcePlanResult.RejectAll(request);
        return rejected with
        {
            ExecutionPlan = SourceExecutionPlan.Empty(request.Identity) with
            {
                Properties = CreateProperties(
                    strategyPropertyName: null,
                    strategy: null,
                    projectionWorkPropertyName,
                    projectionWork)
            }
        };
    }

    public static SourcePredicateExpression? GetFirstConjunct(SourcePredicateExpression? predicate)
    {
        return predicate switch
        {
            SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } and => GetFirstConjunct(and.Left),
            _ => predicate
        };
    }

    public static SourcePredicateExpression? RemoveFirstConjunct(SourcePredicateExpression? predicate)
    {
        if (predicate is not SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical)
            return null;

        var left = RemoveFirstConjunct(logical.Left);
        return left == null
            ? logical.Right
            : logical with { Left = left };
    }

    private static Dictionary<string, object?> CreateProperties(
        string? strategyPropertyName,
        SourcePlanningExecutionStrategy? strategy,
        string? projectionWorkPropertyName,
        bool projectionWork)
    {
        var properties = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(strategyPropertyName) && strategy.HasValue)
            properties[strategyPropertyName] = strategy.Value.ToString();

        if (!string.IsNullOrWhiteSpace(projectionWorkPropertyName) && projectionWork)
            properties[projectionWorkPropertyName] = true;

        return properties;
    }
}
