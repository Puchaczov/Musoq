using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class PlanOptimizationRunner<TPlan>
{
    private const int DefaultMaxIterations = 8;

    private readonly OptimizationStage _stage;
    private readonly IPlanOptimizationPass<TPlan>[] _passes;
    private readonly OptimizationPassRunMode _runMode;
    private readonly int _maxIterations;

    public PlanOptimizationRunner(
        OptimizationStage stage,
        IEnumerable<IPlanOptimizationPass<TPlan>> passes,
        OptimizationPassRunMode runMode = OptimizationPassRunMode.Once,
        int maxIterations = DefaultMaxIterations)
    {
        ArgumentNullException.ThrowIfNull(passes);
        if (maxIterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxIterations), "Maximum iterations must be greater than zero.");

        _stage = stage;
        _passes = passes.ToArray();
        _runMode = runMode;
        _maxIterations = maxIterations;

        foreach (var pass in _passes)
        {
            ArgumentNullException.ThrowIfNull(pass);
            if (string.IsNullOrWhiteSpace(pass.Name))
                throw new ArgumentException("Optimization pass name cannot be null or whitespace.", nameof(passes));
        }
    }

    public PlanOptimizationRunner(OptimizationPassPipeline<TPlan> pipeline, int maxIterations = DefaultMaxIterations)
        : this(
            (pipeline ?? throw new ArgumentNullException(nameof(pipeline))).Stage,
            pipeline.Passes,
            pipeline.RunMode,
            maxIterations)
    {
    }

    public OptimizationResult<TPlan> Run(TPlan plan, OptimizationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (_passes.Length == 0)
            return OptimizationResult<TPlan>.NoChange(plan, "No optimization passes were configured.");

        var runContext = (context ?? new OptimizationContext(_stage)).WithStage(_stage);
        var current = plan;
        var anyChange = false;
        var reachedMaxIterations = false;

        var iterations = _runMode == OptimizationPassRunMode.Once ? 1 : _maxIterations;
        for (var iteration = 1; iteration <= iterations; iteration++)
        {
            var iterationChanged = false;

            foreach (var pass in _passes)
            {
                runContext.AnalysisFacts.BeginPass(_stage, pass.Name, iteration);
                var result = pass.Optimize(current, runContext) ??
                             throw new InvalidOperationException($"Optimization pass {pass.Name} returned null.");

                ArgumentNullException.ThrowIfNull(result.Plan);

                current = result.Plan;
                anyChange |= result.IsChanged;
                iterationChanged |= result.IsChanged;
                var invalidatedFactCount = 0;
                if (result.IsChanged)
                    invalidatedFactCount = runContext.AnalysisFacts.InvalidateForPlanChange();

                runContext.Trace.Add(new OptimizationTraceEntry(
                    _stage,
                    pass.Name,
                    iteration,
                    result.IsChanged,
                    result.Outcome,
                    AppendAnalysisFactDiagnostic(
                        result.Reason,
                        runContext.AnalysisFacts.CreateCurrentPassFactDiagnostic(invalidatedFactCount))));
            }

            if (_runMode == OptimizationPassRunMode.Once || !iterationChanged)
                break;

            reachedMaxIterations = iteration == _maxIterations;
        }

        if (reachedMaxIterations)
        {
            const string reason = "Maximum fixed-point iterations reached.";
            runContext.Trace.Add(new OptimizationTraceEntry(
                _stage,
                "FixedPoint",
                _maxIterations,
                false,
                "MaxIterationsReached",
                reason));

            return new OptimizationResult<TPlan>(current, anyChange, anyChange ? "Changed" : "NoChange", reason);
        }

        return new OptimizationResult<TPlan>(
            current,
            anyChange,
            anyChange ? "Changed" : "NoChange",
            anyChange ? "Optimization passes changed the plan." : "Optimization passes made no changes.");
    }

    private static string AppendAnalysisFactDiagnostic(string reason, string? diagnostic)
    {
        return string.IsNullOrWhiteSpace(diagnostic)
            ? reason
            : $"{reason} {diagnostic}";
    }
}
