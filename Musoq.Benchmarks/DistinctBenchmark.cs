using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

public class DistinctBenchmark : BenchmarkBase
{
    private readonly CompiledQuery _distinctSingleColumn;
    private readonly CompiledQuery _groupBySingleColumn;
    private readonly CompiledQuery _distinctMultipleColumns;
    private readonly CompiledQuery _groupByMultipleColumns;
    private readonly CompiledQuery _distinctWithFilter;
    private readonly CompiledQuery _groupByWithFilter;
    private readonly CompiledQuery _distinctHighCardinality;
    private readonly CompiledQuery _groupByHighCardinality;
    private readonly CompiledQuery _distinctLowCardinality;
    private readonly CompiledQuery _groupByLowCardinality;
    
    public DistinctBenchmark()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var data = DataHelpers.ReadProfiles(contentPath).ToList();
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            {"#A", data}
        };

        _distinctSingleColumn = CreateForProfilesWithOptions(
            "select distinct Gender from #A.Entities()",
            sources,
            new CompilationOptions());

        _groupBySingleColumn = CreateForProfilesWithOptions(
            "select Gender from #A.Entities() group by Gender",
            sources,
            new CompilationOptions());

        _distinctMultipleColumns = CreateForProfilesWithOptions(
            "select distinct Gender, Animal from #A.Entities()",
            sources,
            new CompilationOptions());

        _groupByMultipleColumns = CreateForProfilesWithOptions(
            "select Gender, Animal from #A.Entities() group by Gender, Animal",
            sources,
            new CompilationOptions());

        _distinctWithFilter = CreateForProfilesWithOptions(
            "select distinct Gender, Animal from #A.Entities() where Email like '%.com'",
            sources,
            new CompilationOptions());

        _groupByWithFilter = CreateForProfilesWithOptions(
            "select Gender, Animal from #A.Entities() where Email like '%.com' group by Gender, Animal",
            sources,
            new CompilationOptions());

        _distinctHighCardinality = CreateForProfilesWithOptions(
            "select distinct Email from #A.Entities()",
            sources,
            new CompilationOptions());

        _groupByHighCardinality = CreateForProfilesWithOptions(
            "select Email from #A.Entities() group by Email",
            sources,
            new CompilationOptions());

        _distinctLowCardinality = CreateForProfilesWithOptions(
            "select distinct Gender from #A.Entities()",
            sources,
            new CompilationOptions());

        _groupByLowCardinality = CreateForProfilesWithOptions(
            "select Gender from #A.Entities() group by Gender",
            sources,
            new CompilationOptions());
    }

    [Benchmark]
    public Table DistinctSingleColumn()
    {
        return _distinctSingleColumn.Run();
    }

    [Benchmark]
    public Table GroupBySingleColumn()
    {
        return _groupBySingleColumn.Run();
    }

    [Benchmark]
    public Table DistinctMultipleColumns()
    {
        return _distinctMultipleColumns.Run();
    }

    [Benchmark]
    public Table GroupByMultipleColumns()
    {
        return _groupByMultipleColumns.Run();
    }

    [Benchmark]
    public Table DistinctWithFilter()
    {
        return _distinctWithFilter.Run();
    }

    [Benchmark]
    public Table GroupByWithFilter()
    {
        return _groupByWithFilter.Run();
    }

    [Benchmark]
    public Table DistinctHighCardinality()
    {
        return _distinctHighCardinality.Run();
    }

    [Benchmark]
    public Table GroupByHighCardinality()
    {
        return _groupByHighCardinality.Run();
    }

    [Benchmark]
    public Table DistinctLowCardinality()
    {
        return _distinctLowCardinality.Run();
    }

    [Benchmark]
    public Table GroupByLowCardinality()
    {
        return _groupByLowCardinality.Run();
    }
}
