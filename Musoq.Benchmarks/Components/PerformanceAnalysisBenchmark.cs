using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using System.Reflection;

namespace Musoq.Benchmarks.Components;

/// <summary>
/// Targeted benchmarks for performance analysis and optimization validation
/// </summary>
[MemoryDiagnoser]
public class PerformanceAnalysisBenchmark : BenchmarkBase
{
    private readonly List<ProfileEntity> _profileData;
    private readonly CompiledQuery _simpleSelectQuery;
    private readonly CompiledQuery _filterQuery;
    private readonly CompiledQuery _sortQuery;
    
    public PerformanceAnalysisBenchmark()
    {
        // Load profile data
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        _profileData = DataHelpers.ReadProfiles(contentPath);
        
        // Pre-compile queries for execution benchmarks
        _simpleSelectQuery = CreateQuery("select FirstName, LastName, Email from #A.Entities()");
        _filterQuery = CreateQuery("select FirstName, LastName, Email from #A.Entities() where Email like '%.com'");
        _sortQuery = CreateQuery("select FirstName, LastName, Email from #A.Entities() order by LastName, FirstName");
    }

    // Schema Provider Performance Tests
    [Benchmark]
    public Table SimpleSelect_CurrentSchemaProvider()
    {
        return _simpleSelectQuery.Run();
    }

    [Benchmark]
    public Table FilteredSelect_CurrentSchemaProvider()
    {
        return _filterQuery.Run();
    }

    [Benchmark]
    public Table SortedSelect_CurrentSchemaProvider()
    {
        return _sortQuery.Run();
    }

    // Compilation Performance Tests
    [Benchmark]
    public CompiledQuery Compilation_SimpleSelect()
    {
        return CreateQuery("select FirstName, LastName, Email from #A.Entities()");
    }

    [Benchmark]
    public CompiledQuery Compilation_FilterSelect()
    {
        return CreateQuery("select FirstName, LastName, Email from #A.Entities() where Email like '%.com'");
    }

    [Benchmark]
    public CompiledQuery Compilation_SortSelect()
    {
        return CreateQuery("select FirstName, LastName, Email from #A.Entities() order by LastName");
    }

    // Memory Usage Analysis - Different data sizes
    [Benchmark]
    public Table MemoryTest_SmallDataset()
    {
        return CreateAndRunQuery("select FirstName, LastName from #A.Entities()", _profileData.Take(100));
    }

    [Benchmark]
    public Table MemoryTest_MediumDataset()
    {
        return CreateAndRunQuery("select FirstName, LastName from #A.Entities()", _profileData.Take(1000));
    }

    [Benchmark]
    public Table MemoryTest_LargeDataset()
    {
        return CreateAndRunQuery("select FirstName, LastName from #A.Entities()", _profileData.Take(10000));
    }

    // Parallelization Impact Analysis
    [Benchmark]
    public Table Parallel_Full_LargeDataset()
    {
        return CreateAndRunQueryWithParallelization(
            "select FirstName, LastName, Email from #A.Entities() where Email like '%.com'",
            _profileData.Take(10000),
            ParallelizationMode.Full);
    }

    [Benchmark]
    public Table Parallel_None_LargeDataset()
    {
        return CreateAndRunQueryWithParallelization(
            "select FirstName, LastName, Email from #A.Entities() where Email like '%.com'",
            _profileData.Take(10000),
            ParallelizationMode.None);
    }

    // Reflection vs Direct Access Simulation
    [Benchmark]
    public long ReflectionAccess_PropertyAccess()
    {
        var property = typeof(ProfileEntity).GetProperty("FirstName");
        long total = 0;
        
        foreach (var profile in _profileData.Take(1000))
        {
            var value = (string)property!.GetValue(profile)!;
            total += value.Length;
        }
        
        return total;
    }

    [Benchmark]
    public long DirectAccess_PropertyAccess()
    {
        long total = 0;
        
        foreach (var profile in _profileData.Take(1000))
        {
            total += profile.FirstName.Length;
        }
        
        return total;
    }

    // Helper methods
    private CompiledQuery CreateQuery(string query)
    {
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", _profileData}
        };
        return CreateForProfilesWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
    }

    private Table CreateAndRunQuery(string query, IEnumerable<ProfileEntity> data)
    {
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", data}
        };
        var compiledQuery = CreateForProfilesWithOptions(query, sources, new CompilationOptions(ParallelizationMode.Full));
        return compiledQuery.Run();
    }

    private Table CreateAndRunQueryWithParallelization(string query, IEnumerable<ProfileEntity> data, ParallelizationMode mode)
    {
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", data}
        };
        var compiledQuery = CreateForProfilesWithOptions(query, sources, new CompilationOptions(mode));
        return compiledQuery.Run();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        TokenSource?.Dispose();
    }
}