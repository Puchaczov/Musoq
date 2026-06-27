using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceInteractionPlanner
{
    public static SourceInteractionPlanningResult Plan(
        PlanningContext context,
        IReadOnlyList<SchemaScanNode> scans,
        IReadOnlyDictionary<string, SourcePlanProperties> sources,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scans);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(sourcePredicatePlans);
        var plans = new Dictionary<string, SourceInteractionPlan>(StringComparer.Ordinal);
        var boundaryPlanningResult = SourceBoundaryPlanner.Plan(context.LogicalPlan);
        var decisions = new List<PlanningDecision>();

        foreach (var scan in scans)
        {
            if (string.IsNullOrWhiteSpace(scan.SourceContextId))
                continue;

            if (!sources.TryGetValue(scan.SourceContextId, out var source))
                continue;

            var sourceNode = ResolveSourceNode(context, scan.SourceContextId);
            var usedColumns = ResolveUsedColumns(context, sourceNode, scan.SourceContextId);
            var columns = ResolveColumnContract(source, usedColumns);
            var shape = ResolveShape(context, sourceNode, scan);
            var predicate = ResolvePredicateContract(context, sourceNode, scan.SourceContextId, sourcePredicatePlans);
            var request = ResolveSourcePlanRequest(context, sourceNode, scan.SourceContextId);
            var arguments = ResolveArgumentMode(scan);
            var confidence = ResolveInteractionConfidence(shape.Confidence, columns.Confidence, predicate.Confidence, arguments.Confidence);

            var plan = new SourceInteractionPlan(
                scan.SourceContextId,
                scan.Alias,
                shape.Kind,
                columns.Contract,
                predicate.Contract,
                arguments.Mode,
                columns.Columns,
                predicate.WhereNode,
                request,
                confidence,
                shape.Reason,
                columns.Reason,
                predicate.Reason,
                FormatSourcePlanRequestReason(request),
                arguments.Reason);

            plans[scan.SourceContextId] = plan;
            decisions.Add(CreateDecision(scan, plan));
        }

        decisions.AddRange(boundaryPlanningResult.Decisions);

        return new SourceInteractionPlanningResult(
            plans,
            boundaryPlanningResult.Plans,
            boundaryPlanningResult.StrategyPlans,
            decisions);
    }

    private static SourceInteractionPredicate ResolvePredicateContract(
        PlanningContext context,
        SchemaFromNode? sourceNode,
        string sourceContextId,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans)
    {
        var rawWhereNode = ResolveRawWhereNode(context, sourceNode, sourceContextId);
        var hasRuntimePredicate = rawWhereNode != null && !IsNeutralWhereNode(rawWhereNode);

        if (!sourcePredicatePlans.TryGetValue(sourceContextId, out var predicatePlan))
        {
            if (!hasRuntimePredicate)
            {
                return new SourceInteractionPredicate(
                    SourcePredicateContract.None,
                    rawWhereNode,
                    PlanningConfidence.High,
                    "No source predicate was available.");
            }

            return new SourceInteractionPredicate(
                SourcePredicateContract.RuntimeOnlyPredicate,
                rawWhereNode,
                PlanningConfidence.Low,
                "Source predicate remains runtime-only because no pushdown plan was produced.");
        }

        if (predicatePlan.PushedPredicates.Length > 0)
        {
            return new SourceInteractionPredicate(
                SourcePredicateContract.PushedSourcePredicate,
                predicatePlan.PushedWhereNode,
                predicatePlan.Confidence,
                predicatePlan.Reason);
        }

        if (hasRuntimePredicate)
        {
            return new SourceInteractionPredicate(
                SourcePredicateContract.RuntimeOnlyPredicate,
                predicatePlan.PushedWhereNode,
                predicatePlan.Confidence,
                predicatePlan.Reason);
        }

        return new SourceInteractionPredicate(
            SourcePredicateContract.None,
            predicatePlan.PushedWhereNode,
            predicatePlan.Confidence,
            predicatePlan.Reason);
    }


}
