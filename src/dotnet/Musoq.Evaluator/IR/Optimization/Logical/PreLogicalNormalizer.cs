using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed class PreLogicalNormalizer
{
    public PreLogicalNormalizationResult Normalize(RootNode initialRoot)
    {
        ArgumentNullException.ThrowIfNull(initialRoot);

        var trace = new OptimizationTrace();
        var analysisFacts = new OptimizationAnalysisFactSet();
        var result = new PlanOptimizationRunner<RootNode>(
            PreLogicalNormalizationGroup.Pipeline).Run(
            initialRoot,
            new OptimizationContext(OptimizationStage.PreLogicalNormalization, trace, analysisFacts));

        analysisFacts.TryGet<IReadOnlyList<CorrelatedSubqueryDecision>>(
            CorrelatedSubqueryPlanningFacts.Decisions,
            out var correlatedSubqueryDecisions);

        return new PreLogicalNormalizationResult(
            initialRoot,
            result.Plan,
            trace,
            LogicalSubqueryOwnershipFactCollector.Collect(result.Plan),
            correlatedSubqueryDecisions ?? []);
    }
}

