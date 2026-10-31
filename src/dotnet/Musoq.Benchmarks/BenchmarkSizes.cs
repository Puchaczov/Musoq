using System.Globalization;

namespace Musoq.Benchmarks;

public static class BenchmarkSizes
{
    internal const string SelectionVariable = "MUSOQ_BENCHMARK_CUSTOM_SIZE";
    public static IEnumerable<string> Labels
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable(SelectionVariable);
            if (custom is not null)
                return [$"Custom-{custom}"];
            return ["Small", "Medium", "Large"];
        }
    }

    public static int Count(string size) => size switch
    {
        "Small" => 1_000,
        "Medium" => 10_000,
        "Large" => 100_000,
        _ when size.StartsWith("Custom-", StringComparison.Ordinal) => Parse(size[7..]),
        _ => throw new ArgumentException($"Unknown execution size '{size}'.")
    };

    public static int Parse(string value)
    {
        if (!int.TryParse(value.Replace("_", "", StringComparison.Ordinal), NumberStyles.None,
                CultureInfo.InvariantCulture, out var count) || count <= 0)
            throw new ArgumentException("Size must be a positive integer, for example 1_000.");
        return count;
    }
}
