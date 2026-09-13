using System.Globalization;
using System.Text.Json;

namespace Musoq.Benchmarks.Performance;

internal static class LikeSpecializationQualificationGateCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            var options = Parse(args);
            var result = LikeSpecializationQualificationGate.Evaluate(
                options.LocalBaseline,
                options.LocalCurrent,
                options.CandidateBaseline,
                options.CandidateCurrent);

            Print("local", result.LocalComparisons, output);
            Print("contains", result.ContainsComparisons, output);
            Print("candidate", result.CandidateComparisons, output);
            output.WriteLine(JsonSerializer.Serialize(result));

            if (result.IsSuccess)
            {
                output.WriteLine("LIKE specialization qualification passed.");
                return 0;
            }

            error.WriteLine("LIKE specialization qualification failed.");
            return 1;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or JsonException)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(Usage);
            return 2;
        }
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

    private static Options Parse(IReadOnlyList<string> args)
    {
        var localBaseline = new List<string>();
        var localCurrent = new List<string>();
        var candidateBaseline = new List<string>();
        var candidateCurrent = new List<string>();

        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            if (++index >= args.Count)
                throw new ArgumentException($"Missing value for '{option}'.");

            var reports = option switch
            {
                "--local-baseline" => localBaseline,
                "--local-current" => localCurrent,
                "--candidate-baseline" => candidateBaseline,
                "--candidate-current" => candidateCurrent,
                _ => throw new ArgumentException($"Unknown option '{option}'.")
            };
            reports.Add(args[index]);
        }

        ValidateCount("--local-baseline", localBaseline);
        ValidateCount("--local-current", localCurrent);
        ValidateCount("--candidate-baseline", candidateBaseline);
        ValidateCount("--candidate-current", candidateCurrent);
        return new Options(localBaseline, localCurrent, candidateBaseline, candidateCurrent);
    }

    private static void ValidateCount(string option, IReadOnlyCollection<string> reports)
    {
        if (reports.Count != LikeSpecializationQualificationGate.RequiredReports)
        {
            throw new ArgumentException(
                $"Exactly {LikeSpecializationQualificationGate.RequiredReports} '{option}' arguments are required.");
        }
    }

    private const string Usage =
        "Usage: gate-like-specialization " +
        "--local-baseline <report> (x3) --local-current <report> (x3) " +
        "--candidate-baseline <report> (x3) --candidate-current <report> (x3)";

    private sealed record Options(
        IReadOnlyList<string> LocalBaseline,
        IReadOnlyList<string> LocalCurrent,
        IReadOnlyList<string> CandidateBaseline,
        IReadOnlyList<string> CandidateCurrent);
}
