using System.Globalization;
using System.Text.Json;

namespace Musoq.Benchmarks.Performance;

internal static class RLikeQualificationGateCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            var options = Parse(args);
            var result = RLikeQualificationGate.Evaluate(CreateInputs(options));

            Print("legacy", result.LegacyComparisons, output);
            Print("apply", result.ApplyComparisons, output);
            Print("compilation", result.CompilationComparisons, output);
            Print("constant", result.ConstantComparisons, output);
            Print("dynamic", result.DynamicComparisons, output);
            Print("input-length", result.InputLengthComparisons, output);
            output.WriteLine(JsonSerializer.Serialize(result));

            if (result.IsSuccess)
            {
                output.WriteLine("RLIKE qualification passed.");
                return 0;
            }

            error.WriteLine("RLIKE qualification failed.");
            return 1;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or JsonException)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(Usage);
            return 2;
        }
    }

    private static RLikeQualificationInputs CreateInputs(
        IReadOnlyDictionary<string, string> options) => new(
        CreateReports(options, "legacy"),
        CreateReports(options, "apply"),
        CreateReports(options, "compilation"),
        CreateReports(options, "constant"),
        CreateReports(options, "dynamic"),
        CreateReports(options, "length"));

    private static RLikeBenchmarkReports CreateReports(
        IReadOnlyDictionary<string, string> options,
        string cohort) => new(
        options[$"--{cohort}-baseline"],
        options[$"--{cohort}-current"]);

    private static Dictionary<string, string> Parse(IReadOnlyList<string> args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            if (!OptionNames.Contains(option, StringComparer.Ordinal))
                throw new ArgumentException($"Unknown option '{option}'.");
            if (++index >= args.Count)
                throw new ArgumentException($"Missing value for '{option}'.");
            if (!options.TryAdd(option, args[index]))
                throw new ArgumentException($"Option '{option}' may be specified only once.");
        }

        var missing = OptionNames.Where(option => !options.ContainsKey(option)).ToArray();
        if (missing.Length != 0)
            throw new ArgumentException($"Missing required options: {string.Join(", ", missing)}.");

        return options;
    }

    private static void Print(
        string cohort,
        IEnumerable<BenchmarkComparison> comparisons,
        TextWriter output)
    {
        foreach (var comparison in comparisons)
        {
            var status = Status(comparison);
            output.WriteLine(
                $"{cohort}/{comparison.Method}: " +
                $"time {comparison.TimeRatio.ToString("F4", CultureInfo.InvariantCulture)}x, " +
                $"allocation {comparison.AllocationRatio.ToString("F4", CultureInfo.InvariantCulture)}x {status}");
        }
    }

    private static string Status(BenchmarkComparison comparison)
    {
        if (comparison.IsRegression)
            return "[FAILED]";

        return comparison.TimeRatio < 0.97d || comparison.AllocationRatio < 0.97d
            ? "[IMPROVED]"
            : "[NOISE]";
    }

    private static readonly string[] OptionNames =
    [
        "--legacy-baseline",
        "--legacy-current",
        "--apply-baseline",
        "--apply-current",
        "--compilation-baseline",
        "--compilation-current",
        "--constant-baseline",
        "--constant-current",
        "--dynamic-baseline",
        "--dynamic-current",
        "--length-baseline",
        "--length-current"
    ];

    private const string Usage =
        "Usage: gate-rlike with one baseline/current report for each of " +
        "--legacy, --apply, --compilation, --constant, --dynamic, and --length.";
}
