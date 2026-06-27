using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Logical;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class LogicalOptimizer
{
    private readonly DiagnosticContext? _diagnosticContext;
    private readonly bool _enableConstantFolding;

    public LogicalOptimizer(bool enableConstantFolding = true, DiagnosticContext? diagnosticContext = null)
    {
        _enableConstantFolding = enableConstantFolding;
        _diagnosticContext = diagnosticContext;
    }

    public LogicalOptimizationResult Optimize(LogicalNode initialPlan)
    {
        ArgumentNullException.ThrowIfNull(initialPlan);

        var trace = new OptimizationTrace();
        var options = new OptimizationOptions
        {
            ConstantFoldingEnabled = _enableConstantFolding
        };
        var state = new OptimizationContextState
        {
            DiagnosticContext = _diagnosticContext
        };
        var analysisFacts = new OptimizationAnalysisFactSet();
        var normalizationResult = new PlanOptimizationRunner<LogicalNode>(
            LogicalNormalizationGroup.Pipeline).Run(
            initialPlan,
            new OptimizationContext(OptimizationStage.LogicalNormalization, trace, options, state, analysisFacts));
        var optimizationResult = new PlanOptimizationRunner<LogicalNode>(
            LogicalOptimizationGroup.Pipeline).Run(
            normalizationResult.Plan,
            new OptimizationContext(OptimizationStage.LogicalOptimization, trace, options, state, analysisFacts));

        return new LogicalOptimizationResult(initialPlan, optimizationResult.Plan, trace);
    }
}
