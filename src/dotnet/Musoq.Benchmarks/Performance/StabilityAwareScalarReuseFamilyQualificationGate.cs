using System.Globalization;

namespace Musoq.Benchmarks.Performance;

internal static class StabilityAwareScalarReuseFamilyQualificationGate
{
    public const int RequiredReports = 3;
    public const double MaximumExpensiveHighFanoutRatio = 0.97d;
    public const double MaximumGeneralRatio = 1.03d;
    public const double MaximumAllocationNoiseBytes = 1024d;
    public const int ExpectedComparisonCount = 120;

    private static readonly int[] Fanouts = [1, 8, 64];
    private static readonly string[] Families = Enum.GetNames<ScalarReuseFamily>();
    private static readonly string[] Workloads = Enum.GetNames<ScalarReuseWorkload>();

    public static StabilityAwareScalarReuseFamilyQualificationResult Evaluate(
        IReadOnlyList<string> reportPaths)
    {
        ArgumentNullException.ThrowIfNull(reportPaths);
        if (reportPaths.Count != RequiredReports)
            throw new ArgumentException($"Exactly {RequiredReports} complete reports are required.", nameof(reportPaths));

        var reports = reportPaths.Select(BenchmarkReportReader.Read).ToArray();
        var comparisons = new List<StabilityAwareScalarReuseFamilyQualificationComparison>(ExpectedComparisonCount);
        var failures = new List<string>();
        foreach (var family in Families)
        foreach (var workload in Workloads)
        foreach (var fanout in Fanouts)
        {
            var off = MedianMetric(reports, family, workload, fanout, false, failures);
            var on = MedianMetric(reports, family, workload, fanout, true, failures);
            if (off is null || on is null)
                continue;

            var timeRatio = Ratio(on.MeanNanoseconds, off.MeanNanoseconds);
            var limit = workload == nameof(ScalarReuseWorkload.StableExpensive) && fanout == 64
                ? MaximumExpensiveHighFanoutRatio
                : MaximumGeneralRatio;
            if (timeRatio > limit)
                failures.Add($"{family}/{workload}/fanout={fanout}: time ratio {Format(timeRatio)}x exceeds {Format(limit)}x.");
            if (on.AllocatedBytes > off.AllocatedBytes + MaximumAllocationNoiseBytes)
                failures.Add($"{family}/{workload}/fanout={fanout}: allocation exceeds noise allowance.");

            comparisons.Add(new(
                family,
                workload,
                fanout,
                off,
                on,
                timeRatio,
                Ratio(on.AllocatedBytes, off.AllocatedBytes)));
        }

        return new(comparisons, failures);
    }

    private static BenchmarkMetric? MedianMetric(
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> reports,
        string family,
        string workload,
        int fanout,
        bool enabled,
        ICollection<string> failures)
    {
        var familyToken = $"Family: {family}";
        var workloadToken = $"Workload: {workload}";
        var fanoutToken = $"Fanout: {fanout.ToString(CultureInfo.InvariantCulture)}";
        var methodToken = enabled ? "ExecuteOn(" : "ExecuteOff(";
        var matching = reports.Select(report => report.FirstOrDefault(pair =>
                pair.Key.Contains(familyToken, StringComparison.OrdinalIgnoreCase) &&
                pair.Key.Contains(workloadToken, StringComparison.OrdinalIgnoreCase) &&
                pair.Key.Contains(fanoutToken, StringComparison.OrdinalIgnoreCase) &&
                pair.Key.Contains(methodToken, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (matching.Any(pair => pair.Key is null))
        {
            failures.Add($"Missing complete benchmark row for {family}/{workload}/fanout={fanout}/reuse={enabled}.");
            return null;
        }

        return new(
            Median(matching.Select(pair => pair.Value.MeanNanoseconds)),
            Median(matching.Select(pair => pair.Value.AllocatedBytes)));
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        var midpoint = ordered.Length / 2;
        return ordered.Length % 2 == 1 ? ordered[midpoint] : (ordered[midpoint - 1] + ordered[midpoint]) / 2d;
    }

    private static double Ratio(double current, double baseline) =>
        baseline == 0d ? current == 0d ? 1d : double.PositiveInfinity : current / baseline;

    private static string Format(double value) => value.ToString("F4", CultureInfo.InvariantCulture);
}

internal sealed record StabilityAwareScalarReuseFamilyQualificationResult(
    IReadOnlyList<StabilityAwareScalarReuseFamilyQualificationComparison> Comparisons,
    IReadOnlyList<string> Failures)
{
    public bool IsSuccess => Failures.Count == 0 && Comparisons.Count == StabilityAwareScalarReuseFamilyQualificationGate.ExpectedComparisonCount;
}

internal sealed record StabilityAwareScalarReuseFamilyQualificationComparison(
    string Family,
    string Workload,
    int Fanout,
    BenchmarkMetric Off,
    BenchmarkMetric On,
    double TimeRatio,
    double AllocationRatio);
