using System.Globalization;
using System.Text.Json;

namespace Musoq.Benchmarks.Performance;

internal static class BenchmarkComparisonCommand
{
    private const int RequiredSamples = 3;
    private const double DefaultMaximumRatio = 1.03d;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            var options = Parse(args);
            var result = BenchmarkReportComparer.Compare(
                options.BaselineReports,
                options.CurrentReports,
                options.MaximumTimeRatio,
                options.MaximumAllocationRatio,
                RequiredSamples);

            foreach (var comparison in result.Comparisons)
            {
                var timeRatio = comparison.TimeRatio.ToString("F4", CultureInfo.InvariantCulture);
                var allocationRatio = comparison.AllocationRatio.ToString("F4", CultureInfo.InvariantCulture);
                var status = comparison.IsRegression ? " [REGRESSION]" : string.Empty;
                output.WriteLine($"{comparison.Method}: time {timeRatio}x, allocation {allocationRatio}x{status}");
            }

            if (result.IsSuccess)
            {
                output.WriteLine("Benchmark comparison passed.");
                return 0;
            }

            error.WriteLine("Benchmark comparison failed: one or more methods exceeded the configured ratio.");
            return 1;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or JsonException)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(Usage);
            return 2;
        }
    }

    private static Options Parse(IReadOnlyList<string> args)
    {
        var baselineReports = new List<string>();
        var currentReports = new List<string>();
        var maximumTimeRatio = DefaultMaximumRatio;
        var maximumAllocationRatio = DefaultMaximumRatio;

        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            var value = index + 1 < args.Count
                ? args[++index]
                : throw new ArgumentException($"Missing value for '{option}'.");
            switch (option)
            {
                case "--baseline":
                    baselineReports.Add(value);
                    break;
                case "--current":
                    currentReports.Add(value);
                    break;
                case "--max-time-ratio":
                    maximumTimeRatio = ParseRatio(option, value);
                    break;
                case "--max-allocation-ratio":
                    maximumAllocationRatio = ParseRatio(option, value);
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{option}'.");
            }
        }

        if (baselineReports.Count != RequiredSamples || currentReports.Count != RequiredSamples)
            throw new ArgumentException($"Exactly {RequiredSamples} baseline and {RequiredSamples} current reports are required.");

        return new Options(baselineReports, currentReports, maximumTimeRatio, maximumAllocationRatio);
    }

    private static double ParseRatio(string option, string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ratio) ||
            !double.IsFinite(ratio) ||
            ratio < 1)
        {
            throw new ArgumentException($"'{value}' is not a valid ratio for '{option}'.");
        }

        return ratio;
    }

    private const string Usage =
        "Usage: compare-reports --baseline <report> (x3) --current <report> (x3) " +
        "[--max-time-ratio 1.03] [--max-allocation-ratio 1.03]";

    private sealed record Options(
        IReadOnlyList<string> BaselineReports,
        IReadOnlyList<string> CurrentReports,
        double MaximumTimeRatio,
        double MaximumAllocationRatio);
}
