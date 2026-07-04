using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning.Cardinality;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning.Printing;

internal static partial class PlanningTextPrinter
{
    public static string Print(PlanningResult? result)
    {
        if (result == null)
            return "PlanningUnsupported [planning result was not produced]";

        var builder = new StringBuilder();
        builder.AppendLine("Planning");
        AppendProperties(builder, result.Facts);
        AppendDecisions(builder, result.Decisions);
        return builder.ToString().TrimEnd();
    }

    private static void AppendProperties(StringBuilder builder, PlanningFacts facts)
    {
        builder.AppendLine("  Properties");

        var sourcePlanning = facts.SourcePlanning;
        if (sourcePlanning.SourcesById.Count == 0)
        {
            builder.AppendLine("    Sources: none");
        }
        else
        {
            foreach (var source in Enumerable.OrderBy<SourcePlanProperties, string>(sourcePlanning.SourcesById.Values, static source => source.SourceContextId))
            {
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    Source [{source.SourceContextId}] {source.Alias} -> #{source.SchemaName}.{source.MethodName}");
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      required: {FormatNames(source.RequiredColumns)}");
                AppendRequiredUsages(builder, facts.RequiredColumns, source.SourceContextId);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      pushdown: {FormatPredicates(source.PushedPredicates)}");
                AppendSourcePredicatePlan(builder, sourcePlanning, source.SourceContextId);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      projection: {FormatNames(source.ProjectedColumns)}");
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      shape: {source.ShapeConfidence} ({source.ShapeReason})");
                AppendSourceInteractionPlan(builder, sourcePlanning, source.SourceContextId);
                AppendSourcePlanResult(builder, sourcePlanning, source.SourceContextId);
            }
        }

        AppendRequiredColumnMappings(builder, facts.RequiredColumns);
        AppendRequiredColumnBoundaries(builder, facts.RequiredColumns);
        AppendSourceBoundaries(builder, sourcePlanning);
        AppendSourceBoundaryStrategies(builder, sourcePlanning);
        AppendBoundaryRowShapes(builder, facts.BoundaryPruning);
        AppendRowWidthPruning(builder, facts.BoundaryPruning);
        AppendCardinalityFacts(builder, facts.Cardinality);
        AppendPredicatePlacements(builder, facts.PhysicalStrategies);
        AppendPredicateMovements(builder, facts.PhysicalStrategies);
    }

    private static void AppendRequiredColumnMappings(StringBuilder builder, RequiredColumnFacts requiredColumns)
    {
        builder.AppendLine("    RequiredColumnMappings");

        if (requiredColumns.RequiredColumnMappingPlans.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var plan in Enumerable
                     .OrderBy<RequiredColumnMappingPlan, string>(requiredColumns.RequiredColumnMappingPlans, static plan => plan.SourceContextId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      mapping: {plan.SourceContextId} alias: {plan.Alias} required: {FormatNames(plan.RequiredColumns)} retained: {FormatNames(plan.RetainedColumns)} blocked: {FormatNames(plan.BlockedColumns)} origins: {FormatNames(plan.OriginOutputMappings)} ({plan.Confidence}) - {plan.Reason}");
        }
    }

    private static void AppendRequiredColumnBoundaries(StringBuilder builder, RequiredColumnFacts requiredColumns)
    {
        builder.AppendLine("    RequiredColumnBoundaryFacts");

        if (requiredColumns.RequiredColumnBoundaryPlans.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var plan in Enumerable
                     .OrderBy<RequiredColumnBoundaryPlan, string>(requiredColumns.RequiredColumnBoundaryPlans, static plan => plan.BoundaryId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      boundary: {plan.BoundaryId} {plan.Kind} required: {FormatNames(plan.RequiredColumns)} retained: {FormatNames(plan.RetainedColumns)} blocked: {FormatNames(plan.BlockedColumns)} origins: {FormatNames(plan.OriginOutputMappings)} ({plan.Confidence}) - {plan.Reason}");
        }
    }

    private static void AppendSourceBoundaries(StringBuilder builder, SourcePlanningFacts sourcePlanning)
    {
        builder.AppendLine("    SourceBoundaries");

        if (sourcePlanning.SourceBoundaryPlans.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var plan in Enumerable
                     .OrderBy<SourceBoundaryPlan, string>(sourcePlanning.SourceBoundaryPlans, static plan => plan.BoundaryId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      boundary: {plan.BoundaryId} {plan.Kind} {plan.ApplyKind} {plan.InputMode} target: {plan.Target} inputs: {FormatNames(plan.InputAliases)} outputs: {FormatNames(plan.OutputAliases)} call: {plan.InvocationShape} rows: {plan.RowBehavior} result: {plan.ResultShape} cache: {plan.Cacheability} ({plan.CacheabilityConfidence}) - {plan.Reason}");
        }
    }

    private static void AppendSourceBoundaryStrategies(StringBuilder builder, SourcePlanningFacts sourcePlanning)
    {
        builder.AppendLine("    SourceBoundaryStrategies");

        if (sourcePlanning.SourceBoundaryStrategyPlans.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var plan in Enumerable
                     .OrderBy<SourceBoundaryStrategyPlan, string>(sourcePlanning.SourceBoundaryStrategyPlans, static plan => plan.BoundaryId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      strategy: {plan.BoundaryId} {plan.Kind} {plan.ApplyKind} {plan.InputMode} -> {plan.Strategy} cache: {plan.CachingDecision} ({plan.Confidence}) - {plan.Reason}");
        }
    }

    private static void AppendBoundaryRowShapes(StringBuilder builder, BoundaryPruningFacts boundaryPruning)
    {
        builder.AppendLine("    BoundaryRowShapes");

        if (boundaryPruning.BoundaryRowShapePlans.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var plan in Enumerable
                     .OrderBy<BoundaryRowShapePlan, string>(boundaryPruning.BoundaryRowShapePlans, static plan => plan.BoundaryId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      row-shape: {plan.BoundaryId} {plan.Kind} input: {FormatNames(plan.InputColumns)} after: {FormatNames(plan.NeededAfterBoundaryColumns)} semantic: {FormatNames(plan.SemanticColumns)} retained: {FormatNames(plan.RetainedExecutionColumns)} boundary-only: {FormatNames(plan.BoundaryOnlyColumns)} candidates: {FormatNames(plan.CandidateColumns)} blocked: {FormatNames(plan.BlockedColumns)} droppable-later: {FormatNames(plan.FutureDroppableColumns)} ({plan.Confidence}) - {plan.Reason}");
        }
    }

    private static void AppendRowWidthPruning(StringBuilder builder, BoundaryPruningFacts boundaryPruning)
    {
        builder.AppendLine("    RowWidthPruning");

        if (boundaryPruning.RowWidthPruningPlans.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var plan in Enumerable
                     .OrderBy<RowWidthPruningPlan, string>(boundaryPruning.RowWidthPruningPlans, static plan => plan.BoundaryId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      pruning: {plan.BoundaryId} {plan.Kind} -> {plan.Strategy} candidates: {FormatNames(plan.CandidateColumns)} pruned: {FormatNames(plan.PrunedColumns)} retained: {FormatNames(plan.RetainedColumns)} ({plan.Confidence}) - {plan.Reason}");
        }
    }

    private static void AppendCardinalityFacts(StringBuilder builder, CardinalityPlanningFacts cardinality)
    {
        builder.AppendLine("    CardinalityFacts");

        if (cardinality.Facts.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var fact in Enumerable
                     .OrderBy<CardinalityFact, string>(cardinality.Facts, static fact => fact.TargetKind, StringComparer.Ordinal)
                     .ThenBy(static fact => fact.TargetId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      fact: {fact.TargetId} {fact.TargetKind} -> {fact.Kind} exact={FormatHintValue(fact.ExactRows)} lower={FormatHintValue(fact.LowerBound)} upper={FormatHintValue(fact.UpperBound)} confidence={fact.Confidence} - {fact.Reason}");
        }
    }

    private static void AppendPredicatePlacements(StringBuilder builder, PhysicalStrategyFacts physicalStrategies)
    {
        builder.AppendLine("    PredicatePlacements");

        if (physicalStrategies.PredicatePlacementPlans.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var plan in Enumerable
                     .OrderBy<PredicatePlacementPlan, string>(physicalStrategies.PredicatePlacementPlans, static plan => plan.PredicateId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      placement: {plan.Origin} -> {plan.EarliestPlacement} ({plan.Confidence}) aliases: {FormatNames(plan.Aliases)} predicate: {plan.PredicateText} facts: owners={FormatNames(plan.AliasOwners)} group={plan.ConjunctGroupId} deterministic={FormatBoolean(plan.IsDeterministic)} nulls={plan.NullSensitivity} blocked={FormatNames(plan.BlockedReasons)} - {plan.Reason}");
        }
    }

    private static void AppendPredicateMovements(StringBuilder builder, PhysicalStrategyFacts physicalStrategies)
    {
        builder.AppendLine("    PredicateMovements");

        if (physicalStrategies.PredicateMovementPlans.Count == 0)
        {
            builder.AppendLine("      none");
            return;
        }

        foreach (var plan in Enumerable
                     .OrderBy<PredicateMovementPlan, string>(physicalStrategies.PredicateMovementPlans, static plan => plan.MovementId, StringComparer.Ordinal))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      movement: {plan.Origin} -> PreInnerJoin{plan.Side} ({plan.Confidence}) alias: {plan.Alias} predicate: {plan.PredicateText} - {plan.Reason}");
        }
    }

    private static void AppendSourcePredicatePlan(
        StringBuilder builder,
        SourcePlanningFacts sourcePlanning,
        string sourceContextId)
    {
        if (!sourcePlanning.SourcePredicatePlansBySourceId.TryGetValue(sourceContextId, out var plan))
        {
            builder.AppendLine("      predicate where: none");
            builder.AppendLine("      predicate reason: no source predicate plan was derived (Low)");
            return;
        }

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      predicate where: {plan.PushedWhereNode.Expression.ToString()}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      predicate reason: {plan.Reason} ({plan.Confidence})");
    }

    private static void AppendRequiredUsages(
        StringBuilder builder,
        RequiredColumnFacts requiredColumns,
        string sourceContextId)
    {
        if (!requiredColumns.RequiredColumnUsagesBySourceId.TryGetValue(sourceContextId, out var usages) || usages.Length == 0)
        {
            builder.AppendLine("      usage: none");
            return;
        }

        foreach (var usage in Enumerable
                     .OrderBy<RequiredColumnUsage, string>(usages, static usage => usage.ColumnName)
                     .ThenBy(static usage => usage.UsageReason.ToString())
                     .ThenBy(static usage => usage.Confidence))
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"      usage: {usage.ColumnName} <- {FormatUsageReason(usage.UsageReason)} ({usage.Confidence})");
        }
    }

    private static void AppendDecisions(StringBuilder builder, IReadOnlyList<PlanningDecision> decisions)
    {
        builder.AppendLine("  Decisions");

        if (decisions.Count == 0)
        {
            builder.AppendLine("    none");
            return;
        }

        foreach (var decision in decisions
                     .OrderBy(static decision => decision.Category.ToString())
                     .ThenBy(static decision => decision.RuleName)
                     .ThenBy(static decision => decision.Target)
                     .ThenBy(static decision => decision.Outcome))
        {
            builder
                .Append("    ")
                .Append(decision.Category)
                .Append(" [")
                .Append(decision.RuleName)
                .Append("] ")
                .Append(decision.Target)
                .Append(" -> ")
                .Append(decision.Outcome)
                .Append(" (")
                .Append(decision.Confidence)
                .Append("): ")
                .AppendLine(decision.Reason);
        }
    }

    private static string FormatPredicates(IrExpression[] predicates)
    {
        if (predicates.Length == 0)
            return "none";

        return string.Join(", ", predicates.Select(Expressions.IrExpressionPrinter.Print));
    }

    private static string FormatNames(string[] names)
    {
        return names.Length == 0
            ? "none"
            : string.Join(", ", names);
    }

    private static string FormatColumnNames(ISchemaColumn[] columns)
    {
        return columns.Length == 0
            ? "none"
            : string.Join(", ", columns.Select(static column => column.ColumnName));
    }

    private static string FormatHintValue(long? value)
    {
        return value.HasValue
            ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "null";
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "yes" : "no";
    }

    private static string FormatUsageReason(RequiredColumnUsageReason reason)
    {
        return reason switch
        {
            RequiredColumnUsageReason.Projection => "projection",
            RequiredColumnUsageReason.SourceArgument => "source argument",
            RequiredColumnUsageReason.Where => "where",
            RequiredColumnUsageReason.JoinPredicate => "join predicate",
            RequiredColumnUsageReason.ApplyCorrelation => "apply correlation",
            RequiredColumnUsageReason.GroupBy => "group by",
            RequiredColumnUsageReason.AggregateSetArgument => "aggregate set argument",
            RequiredColumnUsageReason.AggregateGetArgument => "aggregate get argument",
            RequiredColumnUsageReason.Having => "having",
            RequiredColumnUsageReason.OrderBy => "order by",
            RequiredColumnUsageReason.WindowPartition => "window partition",
            RequiredColumnUsageReason.WindowOrder => "window order",
            RequiredColumnUsageReason.WindowValue => "window value",
            RequiredColumnUsageReason.Qualify => "qualify",
            RequiredColumnUsageReason.SetOperationKey => "set-operation key",
            RequiredColumnUsageReason.HiddenIntermediateProjection => "hidden intermediate projection",
            _ => reason.ToString()
        };
    }
}
