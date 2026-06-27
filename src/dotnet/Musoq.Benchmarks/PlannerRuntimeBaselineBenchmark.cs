using System.Globalization;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Helpers;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class PlannerRuntimeBaselineBenchmark : BenchmarkBase
{
    private const int DefaultRowsCount = 5_000;
    private const int OrderedRowsLimit = 100;
    private const string DirectScanProjectionQuery =
        "select FirstName, LastName, Email from #A.Entities()";
    private const string FilteredJoinQuery =
        "select a.Email, b.LastName from #A.Entities() a inner join #B.Entities() b on a.Email = b.Email where a.Gender = 'Female' and b.Email like '%.com'";
    private const string UnionAllQuery =
        "select Email from #A.Entities() union all (Email) select Email from #B.Entities()";
    private const string FilteredUnionAllQuery =
        "select Email from #A.Entities() where Gender = 'Female' union all (Email) select Email from #B.Entities() where Email like '%.com'";
    private const string CteChainQuery =
        "with filtered as (select FirstName, LastName, Email from #A.Entities() where Gender = 'Female'), shaped as (select FirstName, LastName, Email from filtered where Email like '%.com'), final as (select FirstName, LastName, Email from shaped where LastName is not null) select FirstName, LastName, Email from final";
    private const string LeftHashJoinShapeQuery =
        "select a.Email as LeftEmail, b.Email as RightEmail from #A.Entities() a left outer join #B.Entities() b on a.Email = b.Email";
    private const string WindowAggregateShapeQuery =
        "select Gender, Count(Email) as EmailCount, RowNumber() over (order by Count(Email) desc, Gender) as RowNo from #A.Entities() group by Gender order by RowNo";
    private const string CteBackedJoinShapeQuery =
        "with leftCte as (select Email, Gender from #A.Entities()), rightCte as (select Email, LastName from #B.Entities()) select l.Email, l.Gender, r.LastName from leftCte l inner join rightCte r on l.Email = r.Email";
    private const string RepeatedExpressionQuery =
        "select ExpensiveMethod(Value), ExpensiveMethod(Value) + 10, Name from #test.entities() where ExpensiveMethod(Value) > 100";

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _cteChain = null!;
    private CompiledQuery _directScanProjection = null!;
    private CompiledQuery _filteredJoin = null!;
    private CompiledQuery _filteredUnionAll = null!;
    private CompiledQuery _leftHashJoinShape = null!;
    private CompiledQuery _orderTakeMaterialization = null!;
    private CompiledQuery _repeatedExpressionProjection = null!;
    private CompiledQuery _unionAll = null!;
    private CompiledQuery _windowAggregateShape = null!;
    private CompiledQuery _cteBackedJoinShape = null!;

    [Params(DefaultRowsCount)]
    public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var profiles = ReadProfileRows();
        var profileSources = CreateProfileSources(profiles);
        var options = BenchmarkCompilationOptions.Materialized(new CompilationOptions(ParallelizationMode.None));

        _directScanProjection = CreateForProfilesWithOptions(DirectScanProjectionQuery, profileSources, options);
        _filteredJoin = CreateForProfilesWithOptions(FilteredJoinQuery, profileSources, options);
        _unionAll = CreateForProfilesWithOptions(UnionAllQuery, profileSources, options);
        _filteredUnionAll = CreateForProfilesWithOptions(FilteredUnionAllQuery, profileSources, options);
        _cteChain = CreateForProfilesWithOptions(CteChainQuery, profileSources, options);
        _leftHashJoinShape = CreateForProfilesWithOptions(LeftHashJoinShapeQuery, profileSources, options);
        _windowAggregateShape = CreateForProfilesWithOptions(WindowAggregateShapeQuery, profileSources, options);
        _cteBackedJoinShape = CreateForProfilesWithOptions(CteBackedJoinShapeQuery, profileSources, options);
        _orderTakeMaterialization = CreateForProfilesWithOptions(CreateOrderTakeMaterializationQuery(), profileSources, options);
        _repeatedExpressionProjection = CreateRepeatedExpressionQuery(options);
    }

    [Benchmark(Baseline = true)]
    public Table DirectScanProjection()
    {
        return _directScanProjection.Run();
    }

    [Benchmark]
    public Table FilteredJoin()
    {
        return _filteredJoin.Run();
    }

    [Benchmark]
    public Table UnionAllDirectSources()
    {
        return _unionAll.Run();
    }

    [Benchmark]
    public Table FilteredUnionAllDirectSources()
    {
        return _filteredUnionAll.Run();
    }

    [Benchmark]
    public Table CteChainProjection()
    {
        return _cteChain.Run();
    }

    [Benchmark]
    public Table LeftHashJoinShape()
    {
        return _leftHashJoinShape.Run();
    }

    [Benchmark]
    public Table WindowAggregateShape()
    {
        return _windowAggregateShape.Run();
    }

    [Benchmark]
    public Table CteBackedJoinShape()
    {
        return _cteBackedJoinShape.Run();
    }

    [Benchmark]
    public Table OrderTakeMaterialization()
    {
        return _orderTakeMaterialization.Run();
    }

    [Benchmark]
    public Table RepeatedExpressionProjection()
    {
        return _repeatedExpressionProjection.Run();
    }

    private List<ProfileEntity> ReadProfileRows()
    {
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Data", "profiles.csv");
        return DataHelpers.ReadProfiles(contentPath)
            .Take(RowsCount)
            .ToList();
    }

    private static Dictionary<string, IEnumerable<ProfileEntity>> CreateProfileSources(
        IReadOnlyList<ProfileEntity> profiles)
    {
        return new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            ["#A"] = profiles,
            ["#B"] = profiles
        };
    }

    private static string CreateOrderTakeMaterializationQuery()
    {
        return $"select FirstName, LastName, Email from #A.Entities() order by LastName, FirstName take {OrderedRowsLimit.ToString(CultureInfo.InvariantCulture)}";
    }

    private CompiledQuery CreateRepeatedExpressionQuery(CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            RepeatedExpressionQuery,
            Guid.NewGuid().ToString(),
            new CseTestSchemaProvider(CreateRepeatedExpressionRows()),
            _loggerResolver,
            options);
    }

    private List<CseTestEntity> CreateRepeatedExpressionRows()
    {
        return Enumerable.Range(0, RowsCount)
            .Select(index => new CseTestEntity
            {
                Id = index,
                Name = $"Name{index}",
                Value = index % 500,
                Category = $"Category{index % 10}"
            })
            .ToList();
    }
}
