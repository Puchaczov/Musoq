using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
/// Benchmarks to measure the impact of regex caching optimization for LIKE operator.
/// These benchmarks compare query execution with LIKE patterns against baseline queries.
/// </summary>
[MemoryDiagnoser]
public class RegexOptimizationBenchmark : BenchmarkBase
{
    private CompiledQuery _likeQuerySmallDataset = null!;
    private CompiledQuery _likeQueryMediumDataset = null!;
    private CompiledQuery _likeQueryLargeDataset = null!;
    private CompiledQuery _rlikeQuerySmallDataset = null!;
    private CompiledQuery _baselineQuerySmallDataset = null!;
    private CompiledQuery _baselineQueryMediumDataset = null!;
    private CompiledQuery _baselineQueryLargeDataset = null!;
    private CompiledQuery _multipleLikeQuerySmallDataset = null!;

    [GlobalSetup]
    public void Setup()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var allData = DataHelpers.ReadProfiles(contentPath).ToList();
        
        var smallData = allData.Take(1000).ToList();
        var mediumData = allData.Take(10000).ToList();
        var largeData = allData;

        // LIKE query - single pattern (tests regex caching)
        const string likeQuery = "select FirstName, LastName, Email from #A.Entities() where Email like '%.co.uk'";
        
        // RLIKE query - regex pattern
        const string rlikeQuery = "select FirstName, LastName, Email from #A.Entities() where Email rlike '.*\\.co\\.uk$'";
        
        // Multiple LIKE patterns in same query
        const string multipleLikeQuery = "select FirstName, LastName, Email from #A.Entities() where Email like '%.co.uk' or Email like '%.com' or Email like '%.org'";
        
        // Baseline query without pattern matching (for comparison)
        const string baselineQuery = "select FirstName, LastName, Email from #A.Entities() where Gender = 'Male'";

        var smallSources = new Dictionary<string, IEnumerable<ProfileEntity>> { { "#A", smallData } };
        var mediumSources = new Dictionary<string, IEnumerable<ProfileEntity>> { { "#A", mediumData } };
        var largeSources = new Dictionary<string, IEnumerable<ProfileEntity>> { { "#A", largeData } };

        var options = new CompilationOptions(ParallelizationMode.None);

        _likeQuerySmallDataset = CreateForProfilesWithOptions(likeQuery, smallSources, options);
        _likeQueryMediumDataset = CreateForProfilesWithOptions(likeQuery, mediumSources, options);
        _likeQueryLargeDataset = CreateForProfilesWithOptions(likeQuery, largeSources, options);
        
        _rlikeQuerySmallDataset = CreateForProfilesWithOptions(rlikeQuery, smallSources, options);
        
        _multipleLikeQuerySmallDataset = CreateForProfilesWithOptions(multipleLikeQuery, smallSources, options);
        
        _baselineQuerySmallDataset = CreateForProfilesWithOptions(baselineQuery, smallSources, options);
        _baselineQueryMediumDataset = CreateForProfilesWithOptions(baselineQuery, mediumSources, options);
        _baselineQueryLargeDataset = CreateForProfilesWithOptions(baselineQuery, largeSources, options);
    }

    /// <summary>
    /// Baseline query without LIKE - shows overhead of basic query execution
    /// </summary>
    [Benchmark(Baseline = true)]
    public Table Baseline_EqualityFilter_1000Rows()
    {
        return _baselineQuerySmallDataset.Run();
    }

    /// <summary>
    /// Single LIKE pattern on 1000 rows - key metric for regex caching impact
    /// </summary>
    [Benchmark]
    public Table Like_SinglePattern_1000Rows()
    {
        return _likeQuerySmallDataset.Run();
    }

    /// <summary>
    /// Single LIKE pattern on 10000 rows - shows scaling behavior
    /// </summary>
    [Benchmark]
    public Table Like_SinglePattern_10000Rows()
    {
        return _likeQueryMediumDataset.Run();
    }

    /// <summary>
    /// Single LIKE pattern on full dataset - production-like workload
    /// </summary>
    [Benchmark]
    public Table Like_SinglePattern_FullDataset()
    {
        return _likeQueryLargeDataset.Run();
    }

    /// <summary>
    /// RLIKE (regex) pattern - tests regex compilation caching
    /// </summary>
    [Benchmark]
    public Table RLike_Pattern_1000Rows()
    {
        return _rlikeQuerySmallDataset.Run();
    }

    /// <summary>
    /// Multiple LIKE patterns - tests cache with multiple patterns
    /// </summary>
    [Benchmark]
    public Table Like_MultiplePatterns_1000Rows()
    {
        return _multipleLikeQuerySmallDataset.Run();
    }

    /// <summary>
    /// Baseline on medium dataset for comparison
    /// </summary>
    [Benchmark]
    public Table Baseline_EqualityFilter_10000Rows()
    {
        return _baselineQueryMediumDataset.Run();
    }

    /// <summary>
    /// Baseline on large dataset for comparison
    /// </summary>
    [Benchmark]
    public Table Baseline_EqualityFilter_FullDataset()
    {
        return _baselineQueryLargeDataset.Run();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }
}
