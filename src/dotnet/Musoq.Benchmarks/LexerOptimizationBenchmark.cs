using BenchmarkDotNet.Attributes;
using Musoq.Parser.Lexing;

namespace Musoq.Benchmarks;

/// <summary>
///     Micro-benchmarks comparing production span and string keyword lookup.
///     Both methods use the same inputs and token checksum.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class LexerOptimizationBenchmark
{
    private static readonly string[] ProductionKeywordInputs =
    [
        "SELECT", "select", "Select", "FROM", "from", "From", "WHERE", "where", "Where",
        "AND", "and", "And", "EXISTS", "exists", "Exists", "ANY", "some", "All",
        "CONTAINS", "contains", "Contains", "WINDOW", "window", "column_name", "not_a_keyword"
    ];

    [Benchmark(Description = "Production span lookup")]
    public int KeywordLookup_ProductionSpanBased()
    {
        var checksum = 0;
        foreach (var keyword in ProductionKeywordInputs)
        {
            if (KeywordLookup.TryGetKeyword(keyword.AsSpan(), out var tokenType))
                checksum = unchecked((checksum * 31) + (int)tokenType);
        }

        return checksum;
    }

    [Benchmark(Baseline = true, Description = "Production string lookup")]
    public int KeywordLookup_ProductionStringBased()
    {
        var checksum = 0;
        foreach (var keyword in ProductionKeywordInputs)
        {
            if (KeywordLookup.TryGetKeyword(keyword, out var tokenType))
                checksum = unchecked((checksum * 31) + (int)tokenType);
        }

        return checksum;
    }
}
