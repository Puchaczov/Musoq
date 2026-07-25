namespace Musoq.Benchmarks.Performance;

internal static class BenchmarkReportComparer
{
    public static BenchmarkComparisonResult Compare(
        IReadOnlyList<string> baselineReportPaths,
        IReadOnlyList<string> currentReportPaths,
        double maximumTimeRatio,
        double maximumAllocationRatio,
        int minimumSamples = 3,
        Func<string, bool>? methodFilter = null)
    {
        ValidateInputs(
            baselineReportPaths,
            currentReportPaths,
            maximumTimeRatio,
            maximumAllocationRatio,
            minimumSamples);

        var baselineReports = baselineReportPaths.Select(BenchmarkReportReader.Read).ToArray();
        var currentReports = currentReportPaths.Select(BenchmarkReportReader.Read).ToArray();
        var baselineMethods = ValidateCohort("baseline", baselineReports)
            .Where(methodFilter ?? (static _ => true))
            .ToHashSet(StringComparer.Ordinal);
        ValidateCohort("current", currentReports);
        if (baselineMethods.Count == 0)
            throw new InvalidDataException("The benchmark report filter selected no methods.");

        var comparisons = new List<BenchmarkComparison>(baselineMethods.Count);
        foreach (var method in baselineMethods.OrderBy(static method => method, StringComparer.Ordinal))
        {
            if (currentReports.Any(report => !report.ContainsKey(method)))
            {
                throw new InvalidDataException(
                    $"Current benchmark reports do not all contain baseline method '{method}'.");
            }

            var baseline = MedianMetric(baselineReports, method);
            var current = MedianMetric(currentReports, method);
            var timeRatio = Ratio(current.MeanNanoseconds, baseline.MeanNanoseconds);
            var allocationRatio = Ratio(current.AllocatedBytes, baseline.AllocatedBytes);

            comparisons.Add(new BenchmarkComparison(
                method,
                baseline,
                current,
                timeRatio,
                allocationRatio,
                timeRatio > maximumTimeRatio || allocationRatio > maximumAllocationRatio));
        }

        return new BenchmarkComparisonResult(comparisons);
    }

    private static void ValidateInputs(
        IReadOnlyList<string> baselineReportPaths,
        IReadOnlyList<string> currentReportPaths,
        double maximumTimeRatio,
        double maximumAllocationRatio,
        int minimumSamples)
    {
        ArgumentNullException.ThrowIfNull(baselineReportPaths);
        ArgumentNullException.ThrowIfNull(currentReportPaths);

        if (minimumSamples < 1)
            throw new ArgumentOutOfRangeException(nameof(minimumSamples));

        if (baselineReportPaths.Count < minimumSamples)
            throw new ArgumentException($"At least {minimumSamples} baseline reports are required.", nameof(baselineReportPaths));

        if (currentReportPaths.Count < minimumSamples)
            throw new ArgumentException($"At least {minimumSamples} current reports are required.", nameof(currentReportPaths));

        if (!double.IsFinite(maximumTimeRatio) || maximumTimeRatio < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumTimeRatio));

        if (!double.IsFinite(maximumAllocationRatio) || maximumAllocationRatio < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumAllocationRatio));
    }

    private static HashSet<string> ValidateCohort(
        string cohortName,
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> reports)
    {
        var expected = reports[0].Keys.ToHashSet(StringComparer.Ordinal);
        for (var index = 1; index < reports.Count; index++)
        {
            var missing = expected.Except(reports[index].Keys, StringComparer.Ordinal).Order().ToArray();
            var extra = reports[index].Keys.Except(expected, StringComparer.Ordinal).Order().ToArray();
            if (missing.Length == 0 && extra.Length == 0)
                continue;

            throw new InvalidDataException(
                $"The {cohortName} benchmark reports have different method sets at sample {index + 1}. " +
                $"Missing: {FormatMethods(missing)}. Extra: {FormatMethods(extra)}.");
        }

        return expected;
    }

    private static BenchmarkMetric MedianMetric(
        IReadOnlyList<IReadOnlyDictionary<string, BenchmarkMetric>> reports,
        string method)
    {
        return new BenchmarkMetric(
            Median(reports.Select(report => report[method].MeanNanoseconds)),
            Median(reports.Select(report => report[method].AllocatedBytes)));
    }

    private static double Median(IEnumerable<double> source)
    {
        var values = source.Order().ToArray();
        var midpoint = values.Length / 2;
        return values.Length % 2 == 1
            ? values[midpoint]
            : (values[midpoint - 1] + values[midpoint]) / 2d;
    }

    private static double Ratio(double current, double baseline)
    {
        if (baseline == 0)
            return current == 0 ? 1d : double.PositiveInfinity;

        return current / baseline;
    }

    private static string FormatMethods(IReadOnlyCollection<string> methods) =>
        methods.Count == 0 ? "none" : string.Join(", ", methods);
}
