using System.Globalization;

namespace Musoq.Benchmarks.Performance;

internal static class LoopInvariantQualificationGate
{
    public const int RequiredReports = 3;
    public const double MaximumExpensiveHighFanoutRatio = 0.97d;
    public const double MaximumCheapRatio = 1.03d;
    public const double MaximumVolatileRatio = 1.03d;
    // MemoryDiagnoser reports managed bytes at operation granularity. Small
    // runtime/JIT bookkeeping differences between paired measurements are
    // not an allocation introduced by the generated query. Keep the gate
    // sensitive to material regressions while ignoring that measurement noise.
    public const double MaximumAllocationNoiseBytes = 1024d;

    private static readonly int[] Fanouts = [1, 8, 64];

    private static readonly string[] Scenarios =
    [
        "StableCheapGetter",
        "StableExpensiveGetter",
        "VolatileGetter",
        "StableCheapCallable",
        "StableExpensiveCallable",
        "VolatileCallable"
    ];

    public static LoopInvariantQualificationResult Evaluate(IReadOnlyList<string> reportPaths)
    {
        ArgumentNullException.ThrowIfNull(reportPaths);
        if (reportPaths.Count != RequiredReports)
        {
            throw new ArgumentException(
                $"Exactly {RequiredReports} complete BenchmarkDotNet reports are required.",
                nameof(reportPaths));
        }

        var reports = reportPaths.Select(BenchmarkReportReader.Read).ToArray();
        var comparisons = new List<LoopInvariantQualificationComparison>(Scenarios.Length * Fanouts.Length);
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
            var isExpensive = scenario.Contains("Expensive", StringComparison.Ordinal);
            var isVolatile = scenario.Contains("Volatile", StringComparison.Ordinal);
            var maximumTimeRatio = isExpensive && fanout == 64
                ? MaximumExpensiveHighFanoutRatio
                : isExpensive
                    ? (double?)null
                    : isVolatile
                        ? MaximumVolatileRatio
                        : MaximumCheapRatio;

            if (maximumTimeRatio is not null && timeRatio > maximumTimeRatio.Value)
            {
                failures.Add(
                    $"{scenario}/fanout={fanout}: time ratio {Format(timeRatio)}x exceeds {Format(maximumTimeRatio.Value)}x.");
            }

            if (on.AllocatedBytes > off.AllocatedBytes + MaximumAllocationNoiseBytes)
            {
                failures.Add(
                    $"{scenario}/fanout={fanout}: LICM allocation {FormatBytes(on.AllocatedBytes)} exceeds " +
                    $"off allocation {FormatBytes(off.AllocatedBytes)} beyond the " +
                    $"{FormatBytes(MaximumAllocationNoiseBytes)} measurement-noise allowance.");
            }

            comparisons.Add(new LoopInvariantQualificationComparison(
                scenario,
                fanout,
                off,
                on,
                timeRatio,
                allocationRatio));
        }

        return new LoopInvariantQualificationResult(comparisons, failures);
    }

    private static BenchmarkMetric? MedianMetric(
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> reports,
        string scenario,
        int fanout,
        bool enabled,
        ICollection<string> failures)
    {
        var token = $"Scenario: {scenario}";
        var fanoutToken = $"Fanout: {fanout.ToString(CultureInfo.InvariantCulture)}";
        var methodToken = enabled ? "ExecuteOn(" : "ExecuteOff(";
        var matching = reports
            .Select(report => report.FirstOrDefault(pair =>
                pair.Key.Contains(token, StringComparison.OrdinalIgnoreCase) &&
                pair.Key.Contains(fanoutToken, StringComparison.OrdinalIgnoreCase) &&
                pair.Key.Contains(methodToken, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (matching.Any(pair => pair.Key is null))
        {
            failures.Add(
                $"Missing complete benchmark row for {scenario}/fanout={fanout}/licm={enabled}.");
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

    private static double Ratio(double current, double baseline)
    {
        if (baseline == 0)
            return current == 0 ? 1d : double.PositiveInfinity;

        return current / baseline;
    }

    private static string Format(double value) => value.ToString("F4", CultureInfo.InvariantCulture);

    private static string FormatBytes(double value) =>
        $"{value.ToString("F2", CultureInfo.InvariantCulture)} B/op";
}

internal sealed record LoopInvariantQualificationResult(
    IReadOnlyList<LoopInvariantQualificationComparison> Comparisons,
    IReadOnlyList<string> Failures)
{
    public bool IsSuccess => Failures.Count == 0 && Comparisons.Count == 18;
}

internal sealed record LoopInvariantQualificationComparison(
    string Scenario,
    int Fanout,
    BenchmarkMetric Off,
    BenchmarkMetric On,
    double TimeRatio,
    double AllocationRatio);
