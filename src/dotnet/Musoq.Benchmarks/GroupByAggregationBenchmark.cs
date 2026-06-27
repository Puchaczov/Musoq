using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

public class GroupByAggregationBenchmark : BenchmarkBase
{
    private readonly CompiledQuery _countSingleKey;
    private readonly CompiledQuery _countMultipleKeys;
    private readonly CompiledQuery _sumSingleKey;
    private readonly CompiledQuery _multipleAggregatesSingleKey;
    private readonly CompiledQuery _countWithHaving;
    private readonly CompiledQuery _countHighCardinality;
    private readonly CompiledQuery _multiKeyMultipleAggregates;
    private readonly CompiledQuery _customAggregateSingleKey;
    private readonly CompiledQuery _mixedBuiltInAndCustomAggregate;

    public GroupByAggregationBenchmark()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var data = DataHelpers.ReadProfiles(contentPath).ToList();
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            { "#A", data }
        };

        _countSingleKey = CreateForProfilesWithOptions(
            "select Gender, Count(Gender) from #A.Entities() group by Gender",
            sources,
            new CompilationOptions());

        _countMultipleKeys = CreateForProfilesWithOptions(
            "select Gender, Animal, Count(Gender) from #A.Entities() group by Gender, Animal",
            sources,
            new CompilationOptions());

        _sumSingleKey = CreateForProfilesWithOptions(
            "select Gender, Sum(Length(FirstName)) from #A.Entities() group by Gender",
            sources,
            new CompilationOptions());

        _multipleAggregatesSingleKey = CreateForProfilesWithOptions(
            "select Gender, Count(Gender), Sum(Length(FirstName)), Min(Length(Email)), Max(Length(Email)) from #A.Entities() group by Gender",
            sources,
            new CompilationOptions());

        _countWithHaving = CreateForProfilesWithOptions(
            "select Gender, Count(Gender) from #A.Entities() group by Gender having Count(Gender) > 100",
            sources,
            new CompilationOptions());

        _countHighCardinality = CreateForProfilesWithOptions(
            "select Email, Count(Email) from #A.Entities() group by Email",
            sources,
            new CompilationOptions());

        _multiKeyMultipleAggregates = CreateForProfilesWithOptions(
            "select Gender, Animal, Count(Gender), Sum(Length(FirstName)), Max(Length(Email)) from #A.Entities() group by Gender, Animal",
            sources,
            new CompilationOptions());

        _customAggregateSingleKey = CreateForProfilesWithOptions(
            "select Gender, CustomLengthTotal(Length(FirstName)) from #A.Entities() group by Gender",
            sources,
            new CompilationOptions());

        _mixedBuiltInAndCustomAggregate = CreateForProfilesWithOptions(
            "select Gender, Count(Gender), Sum(Length(FirstName)), CustomLengthTotal(Length(Email)) from #A.Entities() group by Gender",
            sources,
            new CompilationOptions());
    }

    [Benchmark]
    public Table CountSingleKey()
    {
        return _countSingleKey.Run();
    }

    [Benchmark]
    public Table CountMultipleKeys()
    {
        return _countMultipleKeys.Run();
    }

    [Benchmark]
    public Table SumSingleKey()
    {
        return _sumSingleKey.Run();
    }

    [Benchmark]
    public Table MultipleAggregatesSingleKey()
    {
        return _multipleAggregatesSingleKey.Run();
    }

    [Benchmark]
    public Table CountWithHaving()
    {
        return _countWithHaving.Run();
    }

    [Benchmark]
    public Table CountHighCardinality()
    {
        return _countHighCardinality.Run();
    }

    [Benchmark]
    public Table MultiKeyMultipleAggregates()
    {
        return _multiKeyMultipleAggregates.Run();
    }

    [Benchmark]
    public Table CustomAggregateSingleKey()
    {
        return _customAggregateSingleKey.Run();
    }

    [Benchmark]
    public Table MixedBuiltInAndCustomAggregate()
    {
        return _mixedBuiltInAndCustomAggregate.Run();
    }
}
