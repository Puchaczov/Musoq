using System.Globalization;

namespace Musoq.Benchmarks.Performance;

internal static class StabilityAwareScalarReuseQualificationGate
{
    public const int RequiredReports = 3;
    public const double MaximumExpensiveHighFanoutRatio = 0.97d;
    public const double MaximumCheapRatio = 1.03d;
    public const double MaximumVolatileRatio = 1.03d;
    public const double MaximumAllocationNoiseBytes = 1024d;
    public const int ExpectedComparisonCount = 12;

    private static readonly int[] Fanouts = [1, 8, 64];

    private static readonly string[] Scenarios =
    [
        "StableCheapFilter",
        "StableExpensiveFilter",
        "StableAggregate",
        "VolatileFilter"
    ];

    public static StabilityAwareScalarReuseQualificationResult Evaluate(
        IReadOnlyList<string> reportPaths)
    {
        ArgumentNullException.ThrowIfNull(reportPaths);
        if (reportPaths.Count != RequiredReports)
        {
            throw new ArgumentException(
                $"Exactly {RequiredReports} complete BenchmarkDotNet reports are required.",
                nameof(reportPaths));
        }

        var reports = reportPaths.Select(BenchmarkReportReader.Read).ToArray();
        var comparisons = new List<StabilityAwareScalarReuseQualificationComparison>(ExpectedComparisonCount);
        var failures = new List<string>();

        foreach (var scenario in Scenarios)
        foreach (var fanout in Fanouts)
        {
            var off = MedianMetric(reports, scenario, fanout, enabled: false, failures);
            var on = MedianMetric(reports, scenario, fanout, enabled: true, failures);
            if (off is null || on is null)
                continue;

            var timeRatio = Ratio(on.MeanNanoseconds, off.MeanNanoseconds);
            var allocationRatio = Ratio(on.AllocatedBytes, off.AllocatedBytes);
            var maximumTimeRatio = scenario == "StableExpensiveFilter" && fanout == 64
                ? MaximumExpensiveHighFanoutRatio
                : scenario == "VolatileFilter"
                    ? MaximumVolatileRatio
                    : MaximumCheapRatio;

            if (timeRatio > maximumTimeRatio)
            {
                failures.Add(
                    $"{scenario}/fanout={fanout}: time ratio {Format(timeRatio)}x exceeds " +
                    $"{Format(maximumTimeRatio)}x.");
            }

            if (on.AllocatedBytes > off.AllocatedBytes + MaximumAllocationNoiseBytes)
            {
                failures.Add(
                    $"{scenario}/fanout={fanout}: reuse allocation {FormatBytes(on.AllocatedBytes)} exceeds " +
                    $"off allocation {FormatBytes(off.AllocatedBytes)} beyond the " +
                    $"{FormatBytes(MaximumAllocationNoiseBytes)} measurement-noise allowance.");
            }

            comparisons.Add(new StabilityAwareScalarReuseQualificationComparison(
                scenario,
                fanout,
                off,
                on,
                timeRatio,
                allocationRatio));
        }

        return new StabilityAwareScalarReuseQualificationResult(comparisons, failures);
    }

    private static BenchmarkMetric? MedianMetric(
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> reports,
        string scenario,
        int fanout,
        bool enabled,
        ICollection<string> failures)
    {
        var scenarioToken = $"Scenario: {scenario}";
        var fanoutToken = $"Fanout: {fanout.ToString(CultureInfo.InvariantCulture)}";
        var methodToken = enabled ? "ExecuteOn(" : "ExecuteOff(";
        var matching = reports
            .Select(report => report.FirstOrDefault(pair =>
                pair.Key.Contains(scenarioToken, StringComparison.OrdinalIgnoreCase) &&
                pair.Key.Contains(fanoutToken, StringComparison.OrdinalIgnoreCase) &&
                pair.Key.Contains(methodToken, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (matching.Any(pair => pair.Key is null))
        {
            failures.Add(
                $"Missing complete benchmark row for {scenario}/fanout={fanout}/reuse={enabled}.");
            return null;
        }

        var metrics = matching.Select(static pair => pair.Value).ToArray();
        return new BenchmarkMetric(
            Median(metrics.Select(static metric => metric.MeanNanoseconds)),
            Median(metrics.Select(static metric => metric.AllocatedBytes)));
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        var midpoint = ordered.Length / 2;
        return ordered.Length % 2 == 1
            ? ordered[midpoint]
            : (ordered[midpoint - 1] + ordered[midpoint]) / 2d;
    }

    private static double Ratio(double current, double baseline) =>
        baseline == 0d ? current == 0d ? 1d : double.PositiveInfinity : current / baseline;

    private static string Format(double value) => value.ToString("F4", CultureInfo.InvariantCulture);

    private static string FormatBytes(double value) =>
        $"{value.ToString("F2", CultureInfo.InvariantCulture)} B/op";
}

internal sealed record StabilityAwareScalarReuseQualificationResult(
    IReadOnlyList<StabilityAwareScalarReuseQualificationComparison> Comparisons,
    IReadOnlyList<string> Failures)
{
    public bool IsSuccess => Failures.Count == 0 &&
                             Comparisons.Count == StabilityAwareScalarReuseQualificationGate.ExpectedComparisonCount;
}

internal sealed record StabilityAwareScalarReuseQualificationComparison(
    string Scenario,
    int Fanout,
    BenchmarkMetric Off,
    BenchmarkMetric On,
    double TimeRatio,
    double AllocationRatio);
