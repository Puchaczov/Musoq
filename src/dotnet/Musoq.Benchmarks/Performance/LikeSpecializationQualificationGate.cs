namespace Musoq.Benchmarks.Performance;

internal static class LikeSpecializationQualificationGate
{
    public const int RequiredReports = 3;

    public static LikeSpecializationQualificationResult Evaluate(
        IReadOnlyList<string> localBaselineReports,
        IReadOnlyList<string> localCurrentReports,
        IReadOnlyList<string> candidateBaselineReports,
        IReadOnlyList<string> candidateCurrentReports)
    {
        var local = BenchmarkReportComparer.Compare(
            localBaselineReports,
            localCurrentReports,
            maximumTimeRatio: 1.03d,
            maximumAllocationRatio: 1.03d,
            RequiredReports);
        var contains = BenchmarkReportComparer.Compare(
            localBaselineReports,
            localCurrentReports,
            maximumTimeRatio: 0.95d,
            maximumAllocationRatio: 1.03d,
            RequiredReports,
            static method => method.Contains("Like_Contains", StringComparison.Ordinal));
        var candidate = BenchmarkReportComparer.Compare(
            candidateBaselineReports,
            candidateCurrentReports,
            maximumTimeRatio: 0.80d,
            maximumAllocationRatio: 1.03d,
            RequiredReports,
            static method => method.Contains("CandidatePayload_SourcePlanningOn", StringComparison.Ordinal));

        return new LikeSpecializationQualificationResult(
            local.Comparisons,
            contains.Comparisons,
            candidate.Comparisons);
    }
}

internal sealed record LikeSpecializationQualificationResult(
    IReadOnlyList<BenchmarkComparison> LocalComparisons,
    IReadOnlyList<BenchmarkComparison> ContainsComparisons,
    IReadOnlyList<BenchmarkComparison> CandidateComparisons)
{
    public bool IsSuccess =>
        LocalComparisons.All(static comparison => !comparison.IsRegression) &&
        ContainsComparisons.All(static comparison => !comparison.IsRegression) &&
        CandidateComparisons.All(static comparison => !comparison.IsRegression);
}
