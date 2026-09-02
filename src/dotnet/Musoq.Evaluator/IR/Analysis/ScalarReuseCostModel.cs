namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Compile-time-only profitability policy for scalar reuse. It never emits a
/// runtime branch or a per-row cache.
/// </summary>
internal sealed record ScalarReuseCostModel(
    int MinimumDynamicUses = 2,
    int MaximumAdditionalPayloadBytes = 1024,
    double MaximumPayloadGrowthRatio = 0.03)
{
    public bool ShouldMaterialize(
        ScalarReuseCandidate candidate,
        int existingRowWidthBytes,
        int predictedOutputUses)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (existingRowWidthBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(existingRowWidthBytes));
        if (predictedOutputUses < 0)
            throw new ArgumentOutOfRangeException(nameof(predictedOutputUses));

        if (!candidate.IsStable || candidate.IsVariableOnly)
            return false;
        if (candidate.UseCount < MinimumDynamicUses && candidate.EstimatedRepeatCount < MinimumDynamicUses)
            return false;
        if (candidate.EstimatedPayloadBytes > MaximumAdditionalPayloadBytes)
            return false;
        if (existingRowWidthBytes > 0 &&
            candidate.EstimatedPayloadBytes > existingRowWidthBytes * MaximumPayloadGrowthRatio)
            return false;

        return predictedOutputUses >= MinimumDynamicUses || candidate.EstimatedRepeatCount >= MinimumDynamicUses;
    }
}
