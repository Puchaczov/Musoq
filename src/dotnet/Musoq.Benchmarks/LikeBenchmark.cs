using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
///     Micro-benchmarks for the SQL LIKE operator.
///     Compares the old compiled-Regex path against the new smart-pattern path
///     (string.EndsWith / StartsWith / Contains / Equals for simple ASCII patterns).
///     End-to-end benchmarks run a real query over 9 453 profile rows.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class LikeBenchmark : BenchmarkBase
{
    private readonly Operators _operators = new();

    // ─── Operator micro-benchmarks (10 000 calls per benchmark) ──────────────

    private const int Iterations = 10_000;

    // Suffix pattern: '%suffix'
    [Benchmark(Description = "LIKE '%suffix' — compiled Regex baseline"), BenchmarkCategory("EndsWith")]
    public bool LikeEndsWith_Regex()
    {
        var b = false;
        for (var i = 0; i < Iterations; i++)
            b = RegexLike("test@example.com", "%.com");
        return b;
    }

    [Benchmark(Description = "LIKE '%suffix' — fast EndsWith path"), BenchmarkCategory("EndsWith")]
    public bool LikeEndsWith_Fast()
    {
        var b = false;
        for (var i = 0; i < Iterations; i++)
            b = _operators.Like("test@example.com", "%.com");
        return b;
    }

    // Prefix pattern: 'prefix%'
    [Benchmark(Description = "LIKE 'prefix%' — compiled Regex baseline"), BenchmarkCategory("StartsWith")]
    public bool LikeStartsWith_Regex()
    {
        var b = false;
        for (var i = 0; i < Iterations; i++)
            b = RegexLike("Testing123", "Test%");
        return b;
    }

    [Benchmark(Description = "LIKE 'prefix%' — fast StartsWith path"), BenchmarkCategory("StartsWith")]
    public bool LikeStartsWith_Fast()
    {
        var b = false;
        for (var i = 0; i < Iterations; i++)
            b = _operators.Like("Testing123", "Test%");
        return b;
    }

    // Contains pattern: '%middle%'
    [Benchmark(Description = "LIKE '%middle%' — compiled Regex baseline"), BenchmarkCategory("Contains")]
    public bool LikeContains_Regex()
    {
        var b = false;
        for (var i = 0; i < Iterations; i++)
            b = RegexLike("hello world goodbye", "%world%");
        return b;
    }

    [Benchmark(Description = "LIKE '%middle%' — fast Contains path"), BenchmarkCategory("Contains")]
    public bool LikeContains_Fast()
    {
        var b = false;
        for (var i = 0; i < Iterations; i++)
            b = _operators.Like("hello world goodbye", "%world%");
        return b;
    }

    // Exact pattern: no wildcards
    [Benchmark(Description = "LIKE 'exact' — compiled Regex baseline"), BenchmarkCategory("Exact")]
    public bool LikeExact_Regex()
    {
        var b = false;
        for (var i = 0; i < Iterations; i++)
            b = RegexLike("Hello", "hello");
        return b;
    }

    [Benchmark(Description = "LIKE 'exact' — fast Equals path"), BenchmarkCategory("Exact")]
    public bool LikeExact_Fast()
    {
        var b = false;
        for (var i = 0; i < Iterations; i++)
            b = _operators.Like("Hello", "hello");
        return b;
    }

    // ─── End-to-end query benchmarks (9 453 profile rows) ─────────────────────

    private CompiledQuery? _endsWith;
    private CompiledQuery? _startsWith;
    private CompiledQuery? _contains;

    [GlobalSetup(Targets = new[] { nameof(Query_EndsWith_Filter), nameof(Query_StartsWith_Filter), nameof(Query_Contains_Filter) })]
    public void SetupEndToEnd()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var data = Helpers.DataHelpers.ReadProfiles(contentPath).ToList();
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>> { { "#A", data } };

        _endsWith = CreateForProfilesWithOptions(
            "select Email from #A.Entities() where Email like '%.com'",
            sources, new CompilationOptions());

        _startsWith = CreateForProfilesWithOptions(
            "select Email from #A.Entities() where Email like 'a%'",
            sources, new CompilationOptions());

        _contains = CreateForProfilesWithOptions(
            "select Email from #A.Entities() where Email like '%john%'",
            sources, new CompilationOptions());
    }

    [Benchmark(Description = "Query: WHERE Email LIKE '%.com'"), BenchmarkCategory("E2E")]
    public int Query_EndsWith_Filter() => _endsWith!.Run().Count;

    [Benchmark(Description = "Query: WHERE Email LIKE 'a%'"), BenchmarkCategory("E2E")]
    public int Query_StartsWith_Filter() => _startsWith!.Run().Count;

    [Benchmark(Description = "Query: WHERE Email LIKE '%john%'"), BenchmarkCategory("E2E")]
    public int Query_Contains_Filter() => _contains!.Run().Count;

    // ─── Reference Regex implementation (baseline for operator benchmarks) ────

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Text.RegularExpressions.Regex>
        RefCache = new();

    private static readonly System.Text.RegularExpressions.Regex EscapeRef =
        new(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static bool RegexLike(string content, string pattern)
    {
        var regex = RefCache.GetOrAdd(pattern, p =>
        {
            var esc = EscapeRef.Replace(p, m => @"\" + m.Value);
            var sql = esc.Replace("_", ".").Replace("%", ".*");
            return new System.Text.RegularExpressions.Regex(
                @"\A" + sql + @"\z",
                System.Text.RegularExpressions.RegexOptions.Singleline |
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Compiled);
        });
        return regex.IsMatch(content);
    }
}