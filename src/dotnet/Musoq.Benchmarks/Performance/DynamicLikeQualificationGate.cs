namespace Musoq.Benchmarks.Performance;

internal static class DynamicLikeQualificationGate
{
    public const int RequiredReports = 3;

    public static DynamicLikeQualificationResult Evaluate(
        IReadOnlyList<string> dynamicBaselineReports,
        IReadOnlyList<string> dynamicCurrentReports,
        IReadOnlyList<string> lengthBaselineReports,
        IReadOnlyList<string> lengthCurrentReports,
        IReadOnlyList<string> applyBaselineReports,
        IReadOnlyList<string> applyCurrentReports,
        IReadOnlyList<string> constantBaselineReports,
        IReadOnlyList<string> constantCurrentReports,
        IReadOnlyList<string> compilationBaselineReports,
        IReadOnlyList<string> compilationCurrentReports)
    {
        return new DynamicLikeQualificationResult(
            Compare(dynamicBaselineReports, dynamicCurrentReports, 0.90d,
                static method => HasParameter(method, "PatternCardinality", 1, 2)),
            Compare(dynamicBaselineReports, dynamicCurrentReports, 0.95d,
                static method => HasParameter(method, "PatternCardinality", 64, 512)),
            Compare(dynamicBaselineReports, dynamicCurrentReports, 1.03d,
                static method => HasParameter(method, "PatternCardinality", 4096)),
            Compare(lengthBaselineReports, lengthCurrentReports, 1.03d, static _ => true),
            Compare(applyBaselineReports, applyCurrentReports, 0.90d,
                static method => method.Contains("DynamicLikeApply_InnerPattern", StringComparison.Ordinal)),
            Compare(applyBaselineReports, applyCurrentReports, 0.80d,
                static method => method.Contains("DynamicLikeApply_OuterPattern", StringComparison.Ordinal)),
            Compare(constantBaselineReports, constantCurrentReports, 0.85d, IsUnicodeOrWildcard),
            Compare(constantBaselineReports, constantCurrentReports, 1.03d, IsConstantAscii),
            Compare(compilationBaselineReports, compilationCurrentReports, 1.05d, static _ => true,
                maximumAllocationRatio: double.MaxValue));
    }

    private static IReadOnlyList<BenchmarkComparison> Compare(
        IReadOnlyList<string> baseline,
        IReadOnlyList<string> current,
        double maximumTimeRatio,
        Func<string, bool> filter,
        double maximumAllocationRatio = 1.03d)
    {
        return BenchmarkReportComparer.Compare(
                baseline,
                current,
                maximumTimeRatio,
                maximumAllocationRatio,
                RequiredReports,
                filter)
            .Comparisons;
    }

    private static bool HasParameter(string method, string parameter, params int[] values)
    {
        return values.Any(value =>
            method.Contains($"{parameter}: {value}", StringComparison.Ordinal) ||
            method.Contains($"{parameter}={value}", StringComparison.Ordinal));
    }

    private static bool IsUnicodeOrWildcard(string method) =>
        method.Contains("Like_Fallback_SingleCharacterWildcard", StringComparison.Ordinal) ||
        method.Contains("Like_Fallback_InteriorWildcard", StringComparison.Ordinal) ||
        method.Contains("Like_Fallback_NonAsciiPattern", StringComparison.Ordinal) ||
        method.Contains("Like_UnicodeInput", StringComparison.Ordinal);

    private static bool IsConstantAscii(string method) =>
        method.Contains("Like_Exact", StringComparison.Ordinal) ||
        method.Contains("Like_Prefix", StringComparison.Ordinal) ||
        method.Contains("Like_Suffix", StringComparison.Ordinal) ||
        method.Contains("Like_Contains", StringComparison.Ordinal) ||
        method.Contains("Like_MultipleMatches", StringComparison.Ordinal);
}

internal sealed record DynamicLikeQualificationResult(
    IReadOnlyList<BenchmarkComparison> LowCardinalityComparisons,
    IReadOnlyList<BenchmarkComparison> MediumCardinalityComparisons,
    IReadOnlyList<BenchmarkComparison> HighCardinalityComparisons,
    IReadOnlyList<BenchmarkComparison> InputLengthComparisons,
    IReadOnlyList<BenchmarkComparison> InnerPatternApplyComparisons,
    IReadOnlyList<BenchmarkComparison> OuterPatternApplyComparisons,
    IReadOnlyList<BenchmarkComparison> UnicodeAndWildcardComparisons,
    IReadOnlyList<BenchmarkComparison> ConstantAsciiComparisons,
    IReadOnlyList<BenchmarkComparison> CompilationComparisons)
{
    public bool IsSuccess => Comparisons.All(static comparison => !comparison.IsRegression);

    public IEnumerable<BenchmarkComparison> Comparisons =>
        LowCardinalityComparisons
            .Concat(MediumCardinalityComparisons)
            .Concat(HighCardinalityComparisons)
            .Concat(InputLengthComparisons)
            .Concat(InnerPatternApplyComparisons)
            .Concat(OuterPatternApplyComparisons)
            .Concat(UnicodeAndWildcardComparisons)
            .Concat(ConstantAsciiComparisons)
            .Concat(CompilationComparisons);
}
