using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceInteractionPlanner
{
    private static PlanningDecision CreateDecision(SchemaScanNode scan, SourceInteractionPlan plan)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.SourceInteraction,
            "SourceInteractionPlan",
            scan.SourceContextId ?? scan.Alias,
            $"{plan.ShapeKind}/{plan.ColumnContract}/{plan.PredicateContract}/{plan.ArgumentMode}",
            plan.Confidence,
            string.Join(" ", plan.ShapeReason, plan.ColumnReason, plan.PredicateReason, plan.SourcePlanRequestReason, plan.ArgumentReason));
    }

    private static PlanningConfidence ResolveInteractionConfidence(params PlanningConfidence[] confidences)
    {
        if (confidences.Any(static confidence => confidence == PlanningConfidence.Low))
            return PlanningConfidence.Low;

        if (confidences.Any(static confidence => confidence == PlanningConfidence.Medium))
            return PlanningConfidence.Medium;

        return PlanningConfidence.High;
    }

    private static SourcePlanRequest ResolveSourcePlanRequest(
        PlanningContext context,
        SchemaFromNode? sourceNode,
        string sourceContextId)
    {
        if (sourceNode != null && context.SourcePlanRequestsBySource.TryGetValue(sourceNode, out var directRequest))
            return directRequest;

        foreach (var entry in context.SourcePlanRequestsBySource)
        {
            if (string.Equals(entry.Key.Id, sourceContextId, StringComparison.Ordinal))
                return entry.Value;
        }

        var identity = sourceNode != null
            ? SourceIdentityFactory.Create(sourceNode)
            : new SourceIdentity(string.Empty, string.Empty, sourceContextId, string.Empty);

        return SourcePlanRequest.Empty(identity);
    }

    private static ISchemaColumn[] ResolveUsedColumns(
        PlanningContext context,
        SchemaFromNode? sourceNode,
        string sourceContextId)
    {
        if (sourceNode != null && context.UsedSchemaColumns.TryGetValue(sourceNode, out var directColumns))
            return directColumns;

        foreach (var entry in context.UsedSchemaColumns)
        {
            if (string.Equals(entry.Key.Id, sourceContextId, StringComparison.Ordinal))
                return entry.Value;
        }

        return [];
    }

    private static WhereNode? ResolveRawWhereNode(
        PlanningContext context,
        SchemaFromNode? sourceNode,
        string sourceContextId)
    {
        if (sourceNode != null && context.UsedWhereNodes.TryGetValue(sourceNode, out var directWhereNode))
            return directWhereNode;

        foreach (var entry in context.UsedWhereNodes)
        {
            if (string.Equals(entry.Key.Id, sourceContextId, StringComparison.Ordinal))
                return entry.Value;
        }

        return null;
    }

    private static SchemaFromNode? ResolveSourceNode(PlanningContext context, string sourceContextId)
    {
        foreach (var sourceNode in context.UsedSchemaColumns.Keys)
        {
            if (string.Equals(sourceNode.Id, sourceContextId, StringComparison.Ordinal))
                return sourceNode;
        }

        foreach (var sourceNode in context.UsedWhereNodes.Keys)
        {
            if (string.Equals(sourceNode.Id, sourceContextId, StringComparison.Ordinal))
                return sourceNode;
        }

        foreach (var sourceNode in context.SourcePlanRequestsBySource.Keys)
        {
            if (string.Equals(sourceNode.Id, sourceContextId, StringComparison.Ordinal))
                return sourceNode;
        }

        return null;
    }

    private static bool IsNeutralWhereNode(WhereNode whereNode)
    {
        return string.Equals(whereNode.Expression.ToString(), "1 = 1", StringComparison.Ordinal);
    }

    private static string FormatSourcePlanRequestReason(SourcePlanRequest request)
    {
        return request.Skip.HasValue || request.Take.HasValue || request.OrderBy.Count > 0
            ? $"Source plan request orderBy={request.OrderBy.Count}, skip={FormatHintValue(request.Skip)}, take={FormatHintValue(request.Take)}."
            : "Source plan request is empty.";
    }

    private static string FormatHintValue(long? value)
    {
        return value.HasValue
            ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "null";
    }
}
