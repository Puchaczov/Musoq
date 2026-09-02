using System.Globalization;
using System.Text.Json;

namespace Musoq.Benchmarks.Performance;

internal static class StabilityAwareScalarReuseQualificationGateCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            var reports = ParseReports(args);
            var result = StabilityAwareScalarReuseQualificationGate.Evaluate(reports);
            foreach (var comparison in result.Comparisons)
            {
                output.WriteLine(
                    $"{comparison.Scenario}/fanout={comparison.Fanout}: " +
                    $"time {comparison.TimeRatio.ToString("F4", CultureInfo.InvariantCulture)}x, " +
                    $"allocation {comparison.AllocationRatio.ToString("F4", CultureInfo.InvariantCulture)}x");
            }

            if (result.Failures.Count != 0)
            {
                foreach (var failure in result.Failures)
                    error.WriteLine(failure);
            }

            output.WriteLine(JsonSerializer.Serialize(result));
            return result.IsSuccess ? 0 : 1;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or JsonException)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(Usage);
            return 2;
        }
    }

    private static IReadOnlyList<string> ParseReports(IReadOnlyList<string> args)
    {
        var reports = new List<string>();
        for (var index = 0; index < args.Count; index++)
        {
            if (!string.Equals(args[index], "--report", StringComparison.Ordinal))
                throw new ArgumentException($"Unknown option '{args[index]}'.");
            if (++index >= args.Count)
                throw new ArgumentException("Missing value for '--report'.");
            reports.Add(args[index]);
        }

        if (reports.Count != StabilityAwareScalarReuseQualificationGate.RequiredReports)
        {
            throw new ArgumentException(
                $"Exactly {StabilityAwareScalarReuseQualificationGate.RequiredReports} '--report' arguments are required.");
        }

        return reports;
    }

    private const string Usage =
        "Usage: gate-stability-aware-reuse --report <benchmarkdotnet-json> (x3)";
}
