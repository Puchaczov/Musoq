using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

public class OrderPaginationBenchmark : BenchmarkBase
{
    private readonly CompiledQuery _orderBySingleKey;
    private readonly CompiledQuery _orderBySkipTake;
    private readonly CompiledQuery _skipTakeWithoutOrder;
    private readonly CompiledQuery _groupByOrderByTake;

    public OrderPaginationBenchmark()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        var data = DataHelpers.ReadProfiles(contentPath).ToList();
        var sources = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            { "#A", data }
        };
        var options = new CompilationOptions(ParallelizationMode.None);

        _orderBySingleKey = CreateForProfilesWithOptions(
            "select FirstName, LastName, Email from #A.Entities() order by LastName",
            sources,
            options);

        _orderBySkipTake = CreateForProfilesWithOptions(
            "select FirstName, LastName, Email from #A.Entities() order by LastName skip 100 take 100",
            sources,
            options);

        _skipTakeWithoutOrder = CreateForProfilesWithOptions(
            "select FirstName, LastName, Email from #A.Entities() skip 100 take 100",
            sources,
            options);

        _groupByOrderByTake = CreateForProfilesWithOptions(
            "select Gender, Count(Gender) from #A.Entities() group by Gender order by Count(Gender) desc take 3",
            sources,
            options);
    }

    [Benchmark]
    public Table OrderBySingleKey()
    {
        return _orderBySingleKey.Run();
    }

    [Benchmark]
    public Table OrderBySkipTake()
    {
        return _orderBySkipTake.Run();
    }

    [Benchmark]
    public Table SkipTakeWithoutOrder()
    {
        return _skipTakeWithoutOrder.Run();
    }

    [Benchmark]
    public Table GroupByOrderByTake()
    {
        return _groupByOrderByTake.Run();
    }
}
