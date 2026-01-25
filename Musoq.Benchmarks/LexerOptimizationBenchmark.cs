using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Baselines;
using Musoq.Parser.Tokens;

namespace Musoq.Benchmarks;

/// <summary>
///     Micro-benchmarks comparing old vs new lexer component implementations.
///     These benchmarks isolate specific optimizations for direct A/B comparison.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class LexerOptimizationBenchmark
{
    // Test keywords of various lengths and cases
    private static readonly string[] TestKeywords =
    [
        "SELECT", "select", "Select",
        "FROM", "from", "From",
        "WHERE", "where", "Where",
        "AND", "and", "And",
        "OR", "or", "Or",
        "DISTINCT", "distinct", "Distinct",
        "GROUP", "group", "Group",
        "ORDER", "order", "Order",
        "HAVING", "having", "Having",
        "INNER", "inner", "Inner",
        "LEFT", "left", "Left",
        "RIGHT", "right", "Right",
        "CROSS", "cross", "Cross",
        "JOIN", "join", "Join",
        "ON", "on", "On",
        "AS", "as", "As",
        "IN", "in", "In",
        "NOT", "not", "Not",
        "IS", "is", "Is",
        "NULL", "null", "Null",
        "TRUE", "true", "True",
        "FALSE", "false", "False",
        "CASE", "case", "Case",
        "WHEN", "when", "When",
        "THEN", "then", "Then",
        "ELSE", "else", "Else",
        "END", "end", "End",
        "LIKE", "like", "Like",
        "CONTAINS", "contains", "Contains",
        "TAKE", "take", "Take",
        "SKIP", "skip", "Skip",
        // Non-keywords (should return Word)
        "myColumn", "tableName", "func123", "col_name"
    ];

    /// <summary>
    ///     Baseline: Switch-based lookup with ToLowerInvariant allocation.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Switch + ToLowerInvariant")]
    public int KeywordLookup_SwitchBased()
    {
        var count = 0;
        foreach (var keyword in TestKeywords)
        {
            var tokenType = LexerBaseline.LookupKeywordWithSwitch(keyword);
            if (tokenType != TokenType.EndOfFile)
                count++;
        }

        return count;
    }

    /// <summary>
    ///     Optimized: FrozenDictionary with OrdinalIgnoreCase (no allocation).
    /// </summary>
    [Benchmark(Description = "FrozenDictionary + OrdinalIgnoreCase")]
    public int KeywordLookup_DictionaryBased()
    {
        var count = 0;
        foreach (var keyword in TestKeywords)
        {
            var tokenType = LexerBaseline.LookupKeywordWithDictionary(keyword);
            if (tokenType != TokenType.EndOfFile)
                count++;
        }

        return count;
    }
}
