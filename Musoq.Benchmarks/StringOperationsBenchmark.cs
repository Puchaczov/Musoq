using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
/// Benchmarks for string operations optimization.
/// Compares performance of Contains, StartsWith, EndsWith operations.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class StringOperationsBenchmark : BenchmarkBase
{
    private CompiledQuery _queryWithContains = null!;
    private CompiledQuery _queryWithStartsWith = null!;
    private CompiledQuery _queryWithEndsWith = null!;
    private CompiledQuery _queryWithMultipleStringOps = null!;
    private CompiledQuery _queryWithEqualityBaseline = null!;
    
    private IReadOnlyList<ProfileEntity> _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        _data = DataHelpers.ReadProfiles(contentPath).Take(10000).ToList();
        
        
        _queryWithEqualityBaseline = CreateQuery(
            "select FirstName, LastName, Email from #A.Entities() where Gender = 'Male'");
        
        
        _queryWithContains = CreateQuery(
            "select FirstName, LastName, Email from #A.Entities() where Contains(Email, 'gmail')");
        
        
        _queryWithStartsWith = CreateQuery(
            "select FirstName, LastName, Email from #A.Entities() where StartsWith(FirstName, 'A')");
        
        
        _queryWithEndsWith = CreateQuery(
            "select FirstName, LastName, Email from #A.Entities() where EndsWith(Email, '.com')");
        
        
        _queryWithMultipleStringOps = CreateQuery(
            "select FirstName, LastName, Email from #A.Entities() where Contains(Email, 'mail') and StartsWith(FirstName, 'J')");
    }

    [Benchmark(Baseline = true)]
    public Table Baseline_EqualityFilter()
    {
        return _queryWithEqualityBaseline.Run();
    }

    [Benchmark]
    public Table Contains_StringSearch()
    {
        return _queryWithContains.Run();
    }

    [Benchmark]
    public Table StartsWith_StringSearch()
    {
        return _queryWithStartsWith.Run();
    }

    [Benchmark]
    public Table EndsWith_StringSearch()
    {
        return _queryWithEndsWith.Run();
    }

    [Benchmark]
    public Table Multiple_StringOperations()
    {
        return _queryWithMultipleStringOps.Run();
    }

    private CompiledQuery CreateQuery(string script)
    {
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", _data}
        };
        
        return CreateForProfilesWithOptions(script, sources, new CompilationOptions(ParallelizationMode.None));
    }
}
