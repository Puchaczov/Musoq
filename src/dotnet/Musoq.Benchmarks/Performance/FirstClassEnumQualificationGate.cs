using System.Globalization;

namespace Musoq.Benchmarks.Performance;

internal static class FirstClassEnumQualificationGate
{
    public const int RequiredReports = 3;
    public const int RowsPerOperation = 8192;
    public const double MaximumAllocationNoiseBytes = 1024d;

    private static readonly FirstClassEnumScenario[] Scenarios =
        Enum.GetValues<FirstClassEnumScenario>();

    public static FirstClassEnumQualificationResult Evaluate(
        IReadOnlyList<string> reportPaths)
    {
        ArgumentNullException.ThrowIfNull(reportPaths);
        if (reportPaths.Count != RequiredReports)
        {
            throw new ArgumentException(
                $"Exactly {RequiredReports} complete BenchmarkDotNet reports are required.",
                nameof(reportPaths));
        }

        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        if (reportPaths
                .Select(Path.GetFullPath)
                .Distinct(pathComparer)
                .Count() != RequiredReports)
        {
            throw new ArgumentException(
                $"Exactly {RequiredReports} distinct BenchmarkDotNet reports are required.",
                nameof(reportPaths));
        }

        var reports = reportPaths.Select(BenchmarkReportReader.Read).ToArray();
        var comparisons = new List<FirstClassEnumQualificationComparison>(Scenarios.Length);
        var failures = new List<string>();

        foreach (var scenario in Scenarios)
        {
            var cohortPairs = ReadCohortPairs(reports, scenario, failures);
            if (cohortPairs is null)
                continue;

            var carrier = MedianMetric(cohortPairs.Select(static pair => pair.Carrier));
            var logicalEnum = MedianMetric(cohortPairs.Select(static pair => pair.LogicalEnum));
            var timeRatio = Median(cohortPairs.Select(static pair =>
                Ratio(pair.LogicalEnum.MeanNanoseconds, pair.Carrier.MeanNanoseconds)));
            var allocationDelta = Median(cohortPairs.Select(static pair =>
                pair.LogicalEnum.AllocatedBytes - pair.Carrier.AllocatedBytes));
            var incrementalBytesPerRow = allocationDelta / RowsPerOperation;
            var maximumTimeRatio = MaximumTimeRatio(scenario);

            if (maximumTimeRatio is not null && timeRatio > maximumTimeRatio.Value)
            {
                failures.Add(
                    $"{scenario}: time ratio {Format(timeRatio)}x exceeds " +
                    $"{Format(maximumTimeRatio.Value)}x.");
            }

            if (allocationDelta > MaximumAllocationNoiseBytes)
            {
                failures.Add(
                    $"{scenario}: enum allocation {FormatBytes(logicalEnum.AllocatedBytes)} exceeds " +
                    $"carrier allocation {FormatBytes(carrier.AllocatedBytes)} by " +
                    $"{FormatBytes(allocationDelta)}, beyond the " +
                    $"{FormatBytes(MaximumAllocationNoiseBytes)} fixed-operation noise allowance.");
            }

            comparisons.Add(new FirstClassEnumQualificationComparison(
                scenario,
                carrier,
                logicalEnum,
                timeRatio,
                allocationDelta,
                incrementalBytesPerRow,
                maximumTimeRatio));
        }

        return new FirstClassEnumQualificationResult(comparisons, failures);
    }

    private static IReadOnlyList<CohortMetricPair>? ReadCohortPairs(
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> reports,
        FirstClassEnumScenario scenario,
        ICollection<string> failures)
    {
        var scenarioToken = $"Scenario: {scenario}";
        var rowsToken = $"RowsCount: {RowsPerOperation.ToString(CultureInfo.InvariantCulture)}";
        var pairs = new List<CohortMetricPair>(reports.Count);
        foreach (var report in reports)
        {
            var carrier = FindMetric(report, scenarioToken, rowsToken, "ExecuteCarrier(");
            var logicalEnum = FindMetric(report, scenarioToken, rowsToken, "ExecuteEnum(");
            if (carrier is null || logicalEnum is null)
            {
                failures.Add($"Missing complete benchmark row for {scenario}.");
                return null;
            }

            pairs.Add(new CohortMetricPair(carrier, logicalEnum));
        }

        return pairs;
    }

    private static BenchmarkMetric? FindMetric(
        IReadOnlyDictionary<string, BenchmarkMetric> report,
        string scenarioToken,
        string rowsToken,
        string methodToken)
    {
        var matching = report.FirstOrDefault(pair =>
            pair.Key.Contains(scenarioToken, StringComparison.OrdinalIgnoreCase) &&
            pair.Key.Contains(rowsToken, StringComparison.OrdinalIgnoreCase) &&
            pair.Key.Contains(methodToken, StringComparison.OrdinalIgnoreCase));
        return matching.Key is null ? null : matching.Value;
    }

    private static BenchmarkMetric MedianMetric(IEnumerable<BenchmarkMetric> metrics)
    {
        var materialized = metrics.ToArray();
        return new BenchmarkMetric(
            Median(materialized.Select(static metric => metric.MeanNanoseconds)),
            Median(materialized.Select(static metric => metric.AllocatedBytes)));
    }

    private static double? MaximumTimeRatio(FirstClassEnumScenario scenario)
    {
        return scenario switch
        {
            FirstClassEnumScenario.Equality or FirstClassEnumScenario.Flags => 1.02d,
            FirstClassEnumScenario.In or FirstClassEnumScenario.Join or
                FirstClassEnumScenario.Grouping or FirstClassEnumScenario.Distinct => 1.03d,
            FirstClassEnumScenario.Helpers or FirstClassEnumScenario.Projection => null,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
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

    private static string Format(double value) =>
        value.ToString("F4", CultureInfo.InvariantCulture);

    private static string FormatBytes(double value) =>
        $"{value.ToString("F2", CultureInfo.InvariantCulture)} B/op";

    private sealed record CohortMetricPair(
        BenchmarkMetric Carrier,
        BenchmarkMetric LogicalEnum);
}

internal sealed record FirstClassEnumQualificationResult(
    IReadOnlyList<FirstClassEnumQualificationComparison> Comparisons,
    IReadOnlyList<string> Failures)
{
    public bool IsSuccess => Failures.Count == 0 &&
                             Comparisons.Count == Enum.GetValues<FirstClassEnumScenario>().Length;
}

internal sealed record FirstClassEnumQualificationComparison(
    FirstClassEnumScenario Scenario,
    BenchmarkMetric Carrier,
    BenchmarkMetric LogicalEnum,
    double TimeRatio,
    double AllocationDeltaBytes,
    double IncrementalBytesPerRow,
    double? MaximumTimeRatio);
