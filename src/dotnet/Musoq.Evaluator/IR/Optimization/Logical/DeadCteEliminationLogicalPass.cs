using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Logical.Rewriting;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed class DeadCteEliminationLogicalPass : ILogicalOptimizationPass
{
    public string Name => "DeadCteElimination";

    public OptimizationResult<LogicalNode> Optimize(LogicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var state = new DeadCteEliminationRewriteState(ConsumeSourceAliasFacts(context));
        var optimized = Rewrite(plan, state);
        if (!ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<LogicalNode>.Changed(
                optimized,
                CreateRemovedDefinitionsReason(
                    state.RemovedDefinitions,
                    state.RemovedSourceBearingDefinitions));
        }

        return OptimizationResult<LogicalNode>.NoChange(
            plan,
            "No dead logical CTE definitions were found.");
    }

    private static LogicalNode Rewrite(LogicalNode node, DeadCteEliminationRewriteState state)
    {
        return node switch
        {
            CteNode cte => RewriteCte(cte, state),
            _ => LogicalPlanRewriter.RewriteChildren(
                node,
                child => Rewrite(child, state))
        };
    }

    private static LogicalNode RewriteCte(CteNode node, DeadCteEliminationRewriteState state)
    {
        var rewrittenDefinitions = new CteDefinition[node.Definitions.Length];
        var definitionsChanged = false;

        for (var index = 0; index < node.Definitions.Length; index++)
        {
            var definition = node.Definitions[index];
            var rewrittenPlan = Rewrite(definition.Plan, state);
            rewrittenDefinitions[index] = ReferenceEquals(rewrittenPlan, definition.Plan)
                ? definition
                : definition with { Plan = rewrittenPlan };
            definitionsChanged |= !ReferenceEquals(rewrittenDefinitions[index], definition);
        }

        var query = Rewrite(node.Query, state);
        var queryChanged = !ReferenceEquals(query, node.Query);

        if (HasDuplicateDefinitionNames(rewrittenDefinitions))
            return definitionsChanged || queryChanged ? new CteNode(rewrittenDefinitions, query) : node;

        var reachableDefinitions = FindReachableDefinitions(rewrittenDefinitions, query);
        if (reachableDefinitions.Count == rewrittenDefinitions.Length)
            return definitionsChanged || queryChanged ? new CteNode(rewrittenDefinitions, query) : node;

        var unreachableDefinitions = rewrittenDefinitions
            .Where(definition => !reachableDefinitions.Contains(definition.Name))
            .ToArray();

        var sourceBearingDefinitions = unreachableDefinitions
            .Where(static definition => LogicalCteUsageFacts.ContainsPlanningSensitiveSource(definition.Plan))
            .ToArray();

        var keptDefinitions = rewrittenDefinitions
            .Where(definition => reachableDefinitions.Contains(definition.Name))
            .ToArray();

        if (sourceBearingDefinitions.Length > 0 &&
            !CanRemoveSourceBearingDefinitions(sourceBearingDefinitions, keptDefinitions, query, state.SourceAliasFacts))
        {
            return definitionsChanged || queryChanged ? new CteNode(rewrittenDefinitions, query) : node;
        }

        state.RemovedDefinitions += rewrittenDefinitions.Length - keptDefinitions.Length;
        state.RemovedSourceBearingDefinitions += sourceBearingDefinitions.Length;

        return keptDefinitions.Length == 0
            ? query
            : new CteNode(keptDefinitions, query);
    }

    private static LogicalSourceAliasFacts? ConsumeSourceAliasFacts(OptimizationContext context)
    {
        return context.AnalysisFacts.TryConsume<LogicalSourceAliasFacts>(
            LogicalAnalysisFactKeys.SourceAndAliasFacts,
            nameof(DeadCteEliminationLogicalPass),
            out var facts)
            ? facts
            : null;
    }

    private sealed class DeadCteEliminationRewriteState(LogicalSourceAliasFacts? sourceAliasFacts)
    {
        public LogicalSourceAliasFacts? SourceAliasFacts { get; } = sourceAliasFacts;

        public int RemovedDefinitions { get; set; }

        public int RemovedSourceBearingDefinitions { get; set; }
    }

    private static bool CanRemoveSourceBearingDefinitions(
        IReadOnlyList<CteDefinition> sourceBearingDefinitions,
        IReadOnlyList<CteDefinition> keptDefinitions,
        LogicalNode query,
        LogicalSourceAliasFacts? facts)
    {
        if (facts is not { HasStableSourceContextAssignments: true, AliasDiagnosticsAreComplete: true })
            return false;

        var removedOrdinals = sourceBearingDefinitions
            .SelectMany(static definition => LogicalSourceOrdinalFacts.CollectSchemaSourceOrdinals(definition.Plan))
            .ToArray();

        if (removedOrdinals.Length == 0)
            return true;

        var keptOrdinals = keptDefinitions
            .SelectMany(static definition => LogicalSourceOrdinalFacts.CollectSchemaSourceOrdinals(definition.Plan))
            .Concat(LogicalSourceOrdinalFacts.CollectSchemaSourceOrdinals(query))
            .ToArray();

        if (keptOrdinals.Length == 0)
            return true;

        var firstRemovedOrdinal = removedOrdinals.Min();
        return keptOrdinals.All(ordinal => ordinal < firstRemovedOrdinal);
    }

    private static string CreateRemovedDefinitionsReason(
        int removedDefinitions,
        int removedSourceBearingDefinitions)
    {
        var baseReason = removedDefinitions == 1
            ? "Removed 1 dead CTE definition."
            : $"Removed {removedDefinitions} dead CTE definition(s).";

        if (removedSourceBearingDefinitions == 0)
            return baseReason;

        var sourceReason = removedSourceBearingDefinitions == 1
            ? " 1 removed definition contained source-bearing logical nodes after source context and alias-scope facts were consumed."
            : $" {removedSourceBearingDefinitions} removed definitions contained source-bearing logical nodes after source context and alias-scope facts were consumed.";

        return baseReason + sourceReason;
    }

    private static bool HasDuplicateDefinitionNames(CteDefinition[] definitions)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return definitions.Any(definition => !names.Add(definition.Name));
    }

    private static HashSet<string> FindReachableDefinitions(CteDefinition[] definitions, LogicalNode query)
    {
        var definitionsByName = definitions.ToDictionary(
            static definition => definition.Name,
            static definition => definition,
            StringComparer.OrdinalIgnoreCase);
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>();

        EnqueueReferencedDefinitions(query, definitionsByName, reachable, pending);

        while (pending.Count > 0)
        {
            var definitionName = pending.Dequeue();
            EnqueueReferencedDefinitions(definitionsByName[definitionName].Plan, definitionsByName, reachable, pending);
        }

        return reachable;
    }

    private static void EnqueueReferencedDefinitions(
        LogicalNode node,
        IReadOnlyDictionary<string, CteDefinition> definitionsByName,
        HashSet<string> reachable,
        Queue<string> pending)
    {
        foreach (var reference in LogicalCteUsageFacts.CollectCteReferences(node))
        {
            if (!definitionsByName.ContainsKey(reference))
                continue;

            if (reachable.Add(reference))
                pending.Enqueue(reference);
        }
    }
}

