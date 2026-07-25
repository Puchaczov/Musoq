namespace Musoq.Benchmarks.Performance;

internal sealed record RecursiveCtePerformanceGateInputs(
    IReadOnlyList<string> BaselineRuntimeReports,
    IReadOnlyList<string> CurrentRuntimeReports,
    IReadOnlyList<string> BaselineCompilationReports,
    IReadOnlyList<string> CurrentCompilationReports,
    IReadOnlyList<string> BaselineOrdinaryCteReports,
    IReadOnlyList<string> CurrentOrdinaryCteReports);

internal sealed record RecursiveCtePerformanceTier(
    string Name,
    double MaximumTimeRatio,
    double MaximumAllocationRatio,
    BenchmarkComparisonResult Result);

internal sealed record RecursiveCtePerformanceGateResult(IReadOnlyList<RecursiveCtePerformanceTier> Tiers)
{
    public bool IsSuccess => Tiers.All(static tier => tier.Result.IsSuccess);
}
