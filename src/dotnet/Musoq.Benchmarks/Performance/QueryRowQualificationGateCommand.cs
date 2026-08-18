using System.Text.Json;

namespace Musoq.Benchmarks.Performance;

internal static class QueryRowQualificationGateCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        try
        {
            var inputs = Parse(args);
            var result = QueryRowQualificationGate.Evaluate(inputs);
            foreach (var check in result.Checks)
            {
                output.WriteLine(
                    $"[{(check.Passed ? "PASS" : "FAIL")}] {check.Name}: {check.Detail}");
            }

            if (result.IsSuccess)
            {
                output.WriteLine("Query-row qualification gate passed.");
                return 0;
            }

            error.WriteLine("Query-row qualification gate failed.");
            return 1;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or JsonException)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(Usage);
            return 2;
        }
    }

    private static QueryRowQualificationInputs Parse(IReadOnlyList<string> args)
    {
        var sourceReports = new List<string>();
        var compiledReports = new List<string>();
        string? disassembly = null;

        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            if (++index >= args.Count)
                throw new ArgumentException($"Missing value for '{option}'.");
            var value = args[index];
            switch (option)
            {
                case "--source-report":
                    sourceReports.Add(value);
                    break;
                case "--compiled-report":
                    compiledReports.Add(value);
                    break;
                case "--disassembly":
                    if (disassembly != null)
                        throw new ArgumentException("--disassembly can be specified only once.");
                    disassembly = value;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{option}'.");
            }
        }

        return new QueryRowQualificationInputs(
            sourceReports.AsReadOnly(),
            compiledReports.AsReadOnly(),
            disassembly ?? throw new ArgumentException("--disassembly is required."));
    }

    private const string Usage =
        "Usage: gate-query-rows --source-report <report> (at least x3) " +
        "--compiled-report <report> (at least x3) --disassembly <jit-disassembly.txt>";
}
