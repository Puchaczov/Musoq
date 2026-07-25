using System.Globalization;
using System.Text.Json;

namespace Musoq.Benchmarks.Performance;

internal static class RecursiveCteBenchmarkGateCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        try
        {
            var options = Parse(args);
            if (options.EquivalenceReports is not null)
            {
                var result = RecursiveCteBenchmarkGate.Compare(options.EquivalenceReports);
                WriteTier(
                    output,
                    new RecursiveCtePerformanceTier(
                        "sequential-equivalence",
                        RecursiveCteBenchmarkGate.MaximumTimeRatio,
                        RecursiveCteBenchmarkGate.MaximumAllocationRatio,
                        result));
                return WriteOutcome(result.IsSuccess, output, error);
            }

            var tiered = RecursiveCteBenchmarkGate.CompareTiered(options.TieredInputs!);
            foreach (var tier in tiered.Tiers)
                WriteTier(output, tier);
            return WriteOutcome(tiered.IsSuccess, output, error);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or JsonException)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(Usage);
            return 2;
        }
    }

    private static int WriteOutcome(bool success, TextWriter output, TextWriter error)
    {
        if (success)
        {
            output.WriteLine("Recursive CTE performance gate passed.");
            return 0;
        }

        error.WriteLine("Recursive CTE performance gate failed.");
        return 1;
    }

    private static void WriteTier(TextWriter output, RecursiveCtePerformanceTier tier)
    {
        output.WriteLine(
            $"[{tier.Name}] limits: time {Format(tier.MaximumTimeRatio)}x, " +
            $"allocation {Format(tier.MaximumAllocationRatio)}x");
        foreach (var comparison in tier.Result.Comparisons)
        {
            var status = comparison.IsRegression ? " [REGRESSION]" : string.Empty;
            output.WriteLine(
                $"{comparison.Method}: time {Format(comparison.TimeRatio)}x, " +
                $"allocation {Format(comparison.AllocationRatio)}x{status}");
        }
    }

    private static string Format(double value) => value.ToString("F4", CultureInfo.InvariantCulture);

    private static Options Parse(IReadOnlyList<string> args)
    {
        var reports = new Dictionary<string, List<string>>(StringComparer.Ordinal)
        {
            ["--report"] = [],
            ["--baseline-runtime"] = [],
            ["--current-runtime"] = [],
            ["--baseline-compilation"] = [],
            ["--current-compilation"] = [],
            ["--baseline-ordinary"] = [],
            ["--current-ordinary"] = []
        };

        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            if (!reports.TryGetValue(option, out var values))
                throw new ArgumentException($"Unknown option '{option}'.");
            if (++index >= args.Count)
                throw new ArgumentException($"Missing value for '{option}'.");
            values.Add(args[index]);
        }

        var equivalenceReports = reports["--report"];
        var tieredReportCount = reports
            .Where(static pair => pair.Key != "--report")
            .Sum(static pair => pair.Value.Count);
        if (equivalenceReports.Count > 0)
        {
            if (tieredReportCount > 0)
                throw new ArgumentException("--report cannot be combined with tiered cohort options.");
            RequireThree("--report", equivalenceReports);
            return new Options(equivalenceReports, null);
        }

        foreach (var pair in reports.Where(static pair => pair.Key != "--report"))
            RequireThree(pair.Key, pair.Value);

        return new Options(
            null,
            new RecursiveCtePerformanceGateInputs(
                reports["--baseline-runtime"],
                reports["--current-runtime"],
                reports["--baseline-compilation"],
                reports["--current-compilation"],
                reports["--baseline-ordinary"],
                reports["--current-ordinary"]));
    }

    private static void RequireThree(string option, IReadOnlyCollection<string> values)
    {
        if (values.Count != RecursiveCteBenchmarkGate.RequiredSamples)
            throw new ArgumentException(
                $"Exactly {RecursiveCteBenchmarkGate.RequiredSamples} {option} values are required.");
    }

    private const string Usage =
        "Usage: gate-recursive --report <current-runtime-report> (x3), or " +
        "--baseline-runtime <report> (x3) --current-runtime <report> (x3) " +
        "--baseline-compilation <report> (x3) --current-compilation <report> (x3) " +
        "--baseline-ordinary <report> (x3) --current-ordinary <report> (x3)";

    private sealed record Options(
        IReadOnlyList<string>? EquivalenceReports,
        RecursiveCtePerformanceGateInputs? TieredInputs);
}
