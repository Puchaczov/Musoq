using System.Collections.Generic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed class PreLogicalNormalizer
{
    public PreLogicalNormalizationResult Normalize(RootNode initialRoot)
    {
        ArgumentNullException.ThrowIfNull(initialRoot);

        // Recursive CTE shape and column-list validation needs the raw syntax.
        // Keep it in this same pass boundary so callers do not walk the AST once
        // for validation and immediately walk it again for normalization.
        RecursiveCtePrevalidator.Validate(initialRoot);

        return NormalizeValidated(initialRoot);
    }

    private static PreLogicalNormalizationResult NormalizeValidated(RootNode initialRoot)
    {
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

    public bool TryNormalize(
        RootNode initialRoot,
        DiagnosticContext diagnostics,
        out PreLogicalNormalizationResult? result)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        try
        {
            RecursiveCtePrevalidator.Validate(initialRoot);
        }
        catch (Exception exception)
        {
            diagnostics.ReportException(exception);
            result = null;
            return false;
        }

        result = NormalizeValidated(initialRoot);
        return true;
    }
}

