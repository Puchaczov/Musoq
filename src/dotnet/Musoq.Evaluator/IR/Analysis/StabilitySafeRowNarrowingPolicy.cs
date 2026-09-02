namespace Musoq.Evaluator.IR.Analysis;

internal sealed record StabilitySafeRowNarrowingEstimate(
    int InputWidthBytes,
    int DroppedWidthBytes,
    bool RetainsVolatileDependencies,
    bool AddsRowObjectAllocation);

internal static class StabilitySafeRowNarrowingPolicy
{
    public const int MinimumBytesRemoved = 16;
    public const double MinimumWidthReduction = 0.25;

    public static bool CanNarrow(StabilitySafeRowNarrowingEstimate estimate)
    {
        ArgumentNullException.ThrowIfNull(estimate);
        if (estimate.InputWidthBytes <= 0 || estimate.DroppedWidthBytes < MinimumBytesRemoved)
            return false;

        var reduction = (double)estimate.DroppedWidthBytes / estimate.InputWidthBytes;
        return reduction >= MinimumWidthReduction &&
               estimate.RetainsVolatileDependencies &&
               !estimate.AddsRowObjectAllocation;
    }
}
