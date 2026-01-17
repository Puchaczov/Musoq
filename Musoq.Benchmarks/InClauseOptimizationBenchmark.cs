using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
/// Benchmarks for IN clause (Contains) optimization.
/// Compares performance of array-based Contains vs HashSet-based lookups.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class InClauseOptimizationBenchmark : BenchmarkBase
{
    private CompiledQuery _queryWithSmallInClause = null!;
    private CompiledQuery _queryWithMediumInClause = null!;
    private CompiledQuery _queryWithLargeInClause = null!;
    private CompiledQuery _queryWithEqualityBaseline = null!;
    private CompiledQuery _queryWithMultipleInClauses = null!;
    
    private IReadOnlyList<ProfileEntity> _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        _data = DataHelpers.ReadProfiles(contentPath).Take(10000).ToList();
        
        
        _queryWithEqualityBaseline = CreateQuery(
            "select FirstName, LastName, Gender from #A.Entities() where Gender = 'Male'");
        
        
        _queryWithSmallInClause = CreateQuery(
            "select FirstName, LastName, Animal from #A.Entities() where Animal in ('Dog', 'Cat', 'Bird')");
        
        
        _queryWithMediumInClause = CreateQuery(
            "select FirstName, LastName, Animal from #A.Entities() where Animal in ('Dog', 'Cat', 'Bird', 'Fish', 'Rabbit', 'Hamster', 'Snake', 'Turtle', 'Horse', 'Pig')");
        
        
        _queryWithLargeInClause = CreateQuery(
            "select FirstName, LastName, Email from #A.Entities() where Gender in ('Male', 'Female', 'Other', 'Unknown', 'Unspecified', 'Non-binary', 'Agender', 'Genderfluid', 'Bigender', 'Two-spirit', 'Androgyne', 'Neutrois', 'Pangender', 'Demigender', 'Genderqueer', 'Third-gender', 'All', 'None', 'Questioning', 'Prefer-not-to-say')");
        
        
        _queryWithMultipleInClauses = CreateQuery(
            "select FirstName, LastName from #A.Entities() where Gender in ('Male', 'Female') and Animal in ('Dog', 'Cat', 'Bird', 'Fish')");
    }

    [Benchmark(Baseline = true)]
    public Table Baseline_EqualityFilter()
    {
        return _queryWithEqualityBaseline.Run();
    }

    [Benchmark]
    public Table InClause_Small_3Values()
    {
        return _queryWithSmallInClause.Run();
    }

    [Benchmark]
    public Table InClause_Medium_10Values()
    {
        return _queryWithMediumInClause.Run();
    }

    [Benchmark]
    public Table InClause_Large_20Values()
    {
        return _queryWithLargeInClause.Run();
    }

    [Benchmark]
    public Table InClause_Multiple()
    {
        return _queryWithMultipleInClauses.Run();
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
