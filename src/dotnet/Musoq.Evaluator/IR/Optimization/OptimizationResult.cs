namespace Musoq.Evaluator.IR.Optimization;

internal sealed record OptimizationResult<TPlan>(
    TPlan Plan,
    bool IsChanged,
    string Outcome,
    string Reason)
{
    public static OptimizationResult<TPlan> NoChange(TPlan plan, string reason)
    {
        return new OptimizationResult<TPlan>(plan, false, "NoChange", reason);
    }

    public static OptimizationResult<TPlan> Changed(TPlan plan, string reason)
    {
        return new OptimizationResult<TPlan>(plan, true, "Changed", reason);
    }
}
