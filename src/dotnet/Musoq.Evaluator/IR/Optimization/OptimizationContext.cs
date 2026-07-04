using Musoq.Evaluator;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class OptimizationContext
{
    public OptimizationContext(
        OptimizationStage stage,
        OptimizationTrace? trace = null,
        OptimizationAnalysisFactSet? analysisFacts = null)
    {
        Stage = stage;
        Trace = trace ?? new OptimizationTrace();
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
        AnalysisFacts = analysisFacts ?? new OptimizationAnalysisFactSet();
        Options = options;
        State = state;
    }

    public OptimizationStage Stage { get; }

    public OptimizationTrace Trace { get; }

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
