using System.Collections.Generic;
using Musoq.Evaluator;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class OptimizationContext
{
    public OptimizationContext(
        OptimizationStage stage,
        OptimizationTrace? trace = null,
        IReadOnlyDictionary<string, object?>? properties = null,
        OptimizationAnalysisFactSet? analysisFacts = null)
    {
        Stage = stage;
        Trace = trace ?? new OptimizationTrace();
        Properties = properties ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        AnalysisFacts = analysisFacts ?? new OptimizationAnalysisFactSet();
        Options = OptimizationOptions.Default;
        State = OptimizationContextState.Empty;
    }

    public OptimizationContext(
        OptimizationStage stage,
        OptimizationTrace? trace,
        OptimizationOptions options,
        OptimizationContextState state,
        OptimizationAnalysisFactSet? analysisFacts = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(state);

        Stage = stage;
        Trace = trace ?? new OptimizationTrace();
        Properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        AnalysisFacts = analysisFacts ?? new OptimizationAnalysisFactSet();
        Options = options;
        State = state;
    }

    public OptimizationStage Stage { get; }

    public OptimizationTrace Trace { get; }

    /// <summary>
    /// Deprecated stringly-typed switch bag. Prefer <see cref="Options"/> and <see cref="State"/>.
    /// Retained as a compatibility path for callers that still build raw property dictionaries.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; }

    public OptimizationAnalysisFactSet AnalysisFacts { get; }

    public OptimizationOptions Options { get; }

    public OptimizationContextState State { get; }

    public OptimizationContext WithStage(OptimizationStage stage)
    {
        if (stage == Stage)
            return this;

        return new OptimizationContext(stage, Trace, Options, State, AnalysisFacts);
    }
}
