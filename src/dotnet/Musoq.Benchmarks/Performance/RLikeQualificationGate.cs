namespace Musoq.Benchmarks.Performance;

internal static class RLikeQualificationGate
{
    public const int RequiredReports = 1;
    public const int ExpectedComparisonCount = 39;
    public const double MaximumRatio = 1.03d;

    public static RLikeQualificationResult Evaluate(RLikeQualificationInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        return new RLikeQualificationResult(
            Compare(inputs.Legacy, "RegexOptimizationBenchmark", 1),
            Compare(inputs.Apply, "RLikeApplyBenchmark", 6),
            Compare(inputs.Compilation, "RLikeCompilationBenchmark", 2),
            Compare(inputs.Constant, "RLikeConstantBenchmark", 4),
            Compare(inputs.Dynamic, "RLikeExecutionBenchmark", 20),
            Compare(inputs.InputLength, "RLikeInputLengthBenchmark", 6));
    }

    private static IReadOnlyList<BenchmarkComparison> Compare(
        RLikeBenchmarkReports reports,
        string benchmarkType,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ValidateReport(reports.Baseline, "baseline", benchmarkType, expectedCount);
        ValidateReport(reports.Current, "current", benchmarkType, expectedCount);

        return BenchmarkReportComparer.Compare(
                [reports.Baseline],
                [reports.Current],
                MaximumRatio,
                MaximumRatio,
                RequiredReports)
            .Comparisons;
    }

    private static void ValidateReport(
        string path,
        string cohort,
        string benchmarkType,
        int expectedCount)
    {
        var methods = BenchmarkReportReader.Read(path).Keys.ToArray();
        if (methods.Length != expectedCount ||
            methods.Any(method => !method.Contains(benchmarkType, StringComparison.Ordinal)))
        {
            throw new InvalidDataException(
                $"The {cohort} {benchmarkType} report must contain exactly {expectedCount} matching benchmarks.");
        }
    }
}

internal sealed record RLikeBenchmarkReports(string Baseline, string Current);

internal sealed record RLikeQualificationInputs(
    RLikeBenchmarkReports Legacy,
    RLikeBenchmarkReports Apply,
    RLikeBenchmarkReports Compilation,
    RLikeBenchmarkReports Constant,
    RLikeBenchmarkReports Dynamic,
    RLikeBenchmarkReports InputLength);

internal sealed record RLikeQualificationResult(
    IReadOnlyList<BenchmarkComparison> LegacyComparisons,
    IReadOnlyList<BenchmarkComparison> ApplyComparisons,
    IReadOnlyList<BenchmarkComparison> CompilationComparisons,
    IReadOnlyList<BenchmarkComparison> ConstantComparisons,
    IReadOnlyList<BenchmarkComparison> DynamicComparisons,
    IReadOnlyList<BenchmarkComparison> InputLengthComparisons)
{
    public bool IsSuccess => Comparisons.Count == RLikeQualificationGate.ExpectedComparisonCount &&
                             Comparisons.All(static comparison => !comparison.IsRegression);

    public IReadOnlyList<BenchmarkComparison> Comparisons =>
        LegacyComparisons
            .Concat(ApplyComparisons)
            .Concat(CompilationComparisons)
            .Concat(ConstantComparisons)
            .Concat(DynamicComparisons)
            .Concat(InputLengthComparisons)
            .ToArray();
}
