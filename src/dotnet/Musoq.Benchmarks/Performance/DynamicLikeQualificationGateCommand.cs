using System.Globalization;
using System.Text.Json;

namespace Musoq.Benchmarks.Performance;

internal static class DynamicLikeQualificationGateCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            var options = Parse(args);
            var result = DynamicLikeQualificationGate.Evaluate(
                options["--dynamic-baseline"],
                options["--dynamic-current"],
                options["--length-baseline"],
                options["--length-current"],
                options["--apply-baseline"],
                options["--apply-current"],
                options["--constant-baseline"],
                options["--constant-current"],
                options["--compilation-baseline"],
                options["--compilation-current"]);

            Print("low-cardinality", result.LowCardinalityComparisons, output);
            Print("medium-cardinality", result.MediumCardinalityComparisons, output);
            Print("high-cardinality", result.HighCardinalityComparisons, output);
            Print("input-length", result.InputLengthComparisons, output);
            Print("inner-pattern-apply", result.InnerPatternApplyComparisons, output);
            Print("outer-pattern-apply", result.OuterPatternApplyComparisons, output);
            Print("unicode-wildcard", result.UnicodeAndWildcardComparisons, output);
            Print("constant-ascii", result.ConstantAsciiComparisons, output);
            Print("compilation", result.CompilationComparisons, output);
            output.WriteLine(JsonSerializer.Serialize(result));

            if (result.IsSuccess)
            {
                output.WriteLine("Dynamic LIKE qualification passed.");
                return 0;
            }

            error.WriteLine("Dynamic LIKE qualification failed.");
            return 1;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or JsonException)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(Usage);
            return 2;
        }
    }

    private static Dictionary<string, IReadOnlyList<string>> Parse(IReadOnlyList<string> args)
    {
        var values = OptionNames.ToDictionary(
            static name => name,
            static _ => new List<string>(),
            StringComparer.Ordinal);
        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            if (!values.TryGetValue(option, out var reports))
                throw new ArgumentException($"Unknown option '{option}'.");
            if (++index >= args.Count)
                throw new ArgumentException($"Missing value for '{option}'.");

            reports.Add(args[index]);
        }

        foreach (var (option, reports) in values)
        {
            if (reports.Count != DynamicLikeQualificationGate.RequiredReports)
            {
                throw new ArgumentException(
                    $"Exactly {DynamicLikeQualificationGate.RequiredReports} '{option}' arguments are required.");
            }
        }

        return values.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<string>)pair.Value,
            StringComparer.Ordinal);
    }

    private static void Print(
        string cohort,
        IEnumerable<BenchmarkComparison> comparisons,
        TextWriter output)
    {
        foreach (var comparison in comparisons)
        {
            var status = comparison.IsRegression ? " [FAILED]" : string.Empty;
            output.WriteLine(
                $"{cohort}/{comparison.Method}: " +
                $"time {comparison.TimeRatio.ToString("F4", CultureInfo.InvariantCulture)}x, " +
                $"allocation {comparison.AllocationRatio.ToString("F4", CultureInfo.InvariantCulture)}x{status}");
        }
    }

    private static readonly string[] OptionNames =
    [
        "--dynamic-baseline",
        "--dynamic-current",
        "--length-baseline",
        "--length-current",
        "--apply-baseline",
        "--apply-current",
        "--constant-baseline",
        "--constant-current",
        "--compilation-baseline",
        "--compilation-current"
    ];

    private const string Usage =
        "Usage: gate-dynamic-like with exactly three reports for each of " +
        "--dynamic-baseline/current, --length-baseline/current, --apply-baseline/current, " +
        "--constant-baseline/current, and --compilation-baseline/current.";
}
