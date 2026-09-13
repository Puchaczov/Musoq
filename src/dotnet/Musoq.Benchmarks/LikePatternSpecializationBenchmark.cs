using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
/// Measures constant LIKE shapes through the complete query compilation and execution path.
/// The method names are intentionally stable so reports can be compared before and after
/// pattern specialization.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class LikePatternSpecializationBenchmark : BenchmarkBase
{
    private CompiledQuery _contains = null!;
    private CompiledQuery _dynamicPattern = null!;
    private CompiledQuery _exact = null!;
    private CompiledQuery _interiorWildcard = null!;
    private CompiledQuery _multipleMatches = null!;
    private CompiledQuery _nonAsciiPattern = null!;
    private CompiledQuery _prefix = null!;
    private CompiledQuery _singleCharacterWildcard = null!;
    private CompiledQuery _suffix = null!;
    private CompiledQuery _unicodeInput = null!;

    [Params(10_000, 100_000)]
    public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = Enumerable.Range(0, RowsCount)
            .Select(CreateRow)
            .ToArray();
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            ["#A"] = rows
        };
        var options = new CompilationOptions(ParallelizationMode.None);

        _exact = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like 'Google0@example.com'",
            sources,
            options);
        _prefix = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like 'Google%'",
            sources,
            options);
        _suffix = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like '%@example.com'",
            sources,
            options);
        _contains = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like '%Google%'",
            sources,
            options);
        _multipleMatches = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like 'Google%' and p.Email like '%@example.com'",
            sources,
            options);
        _singleCharacterWildcard = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like 'Google_@example.com'",
            sources,
            options);
        _interiorWildcard = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like 'Google%@example.com'",
            sources,
            options);
        _nonAsciiPattern = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like '%Góogle%'",
            sources,
            options);
        _dynamicPattern = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like FirstName",
            sources,
            options);

        var unicodeSources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            ["#A"] = Enumerable.Range(0, RowsCount).Select(CreateUnicodeRow).ToArray()
        };
        _unicodeInput = CreateForProfilesWithOptions(
            "select p.Email from #A.Entities() p where p.Email like 'K%'",
            unicodeSources,
            options);
    }

    [Benchmark]
    public Table Like_Exact() => _exact.Run();

    [Benchmark]
    public Table Like_Prefix() => _prefix.Run();

    [Benchmark]
    public Table Like_Suffix() => _suffix.Run();

    [Benchmark(Baseline = true)]
    public Table Like_Contains() => _contains.Run();

    [Benchmark]
    public Table Like_MultipleMatches() => _multipleMatches.Run();

    [Benchmark]
    public Table Like_Fallback_SingleCharacterWildcard() => _singleCharacterWildcard.Run();

    [Benchmark]
    public Table Like_Fallback_InteriorWildcard() => _interiorWildcard.Run();

    [Benchmark]
    public Table Like_Fallback_NonAsciiPattern() => _nonAsciiPattern.Run();

    [Benchmark]
    public Table Like_Fallback_DynamicPattern() => _dynamicPattern.Run();

    [Benchmark]
    public Table Like_UnicodeInput() => _unicodeInput.Run();

    private static ProfileEntity CreateRow(int index)
    {
        var email = index % 10 == 0
            ? $"Google{index}@example.com"
            : $"person{index}@example.org";

        return new ProfileEntity(
            index % 2 == 0 ? "Google%" : "%@example.org",
            "Last",
            email,
            "Unknown",
            "127.0.0.1",
            "2026-01-01",
            "image",
            "animal",
            "avatar")
        {
            FirstName = index % 2 == 0 ? "Google%" : "%@example.org",
            LastName = "Last",
            Email = email,
            Gender = "Unknown",
            IpAddress = "127.0.0.1",
            Date = "2026-01-01",
            Image = "image",
            Animal = "animal",
            Avatar = "avatar"
        };
    }

    private static ProfileEntity CreateUnicodeRow(int index)
    {
        var email = index % 2 == 0
            ? $"Łelvin{index}@example.com"
            : $"Kilo{index}@example.com";

        return new ProfileEntity(
            "K%",
            "Last",
            email,
            "Unknown",
            "127.0.0.1",
            "2026-01-01",
            "image",
            "animal",
            "avatar")
        {
            FirstName = "K%",
            LastName = "Last",
            Email = email,
            Gender = "Unknown",
            IpAddress = "127.0.0.1",
            Date = "2026-01-01",
            Image = "image",
            Animal = "animal",
            Avatar = "avatar"
        };
    }
}
