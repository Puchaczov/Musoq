namespace Musoq.Evaluator.IR.Planning;

internal sealed record ParallelEligibilityCheck(bool IsEligible, string Reason)
{
    public static ParallelEligibilityCheck Enabled { get; } = new(true, string.Empty);

    public static ParallelEligibilityCheck Skipped(string reason)
    {
        return new ParallelEligibilityCheck(false, reason);
    }
}
