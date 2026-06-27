using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>
///     Manual comparison benchmark for opt-in CTE sidecar indexes.
///     Compare baseline and optimized pairs by mean time and allocation deltas;
///     no hard threshold is intended for CI.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class CteSidecarIndexBenchmark
{
    private static readonly CompilationOptions BaselineOptions = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(
            parallelizationMode: ParallelizationMode.None,
            useHashJoin: true,
            useSortMergeJoin: false));

    private static readonly CompilationOptions OptimizedOptions = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(
            parallelizationMode: ParallelizationMode.None,
            useHashJoin: true,
            useSortMergeJoin: false,
            useCteSidecarIndexes: true));

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _singleHashBaseline = null!;
    private CompiledQuery _singleHashOptimized = null!;
    private CompiledQuery _repeatedSelfJoinBaseline = null!;
    private CompiledQuery _repeatedSelfJoinOptimized = null!;
    private CompiledQuery _multiConsumerBaseline = null!;
    private CompiledQuery _multiConsumerOptimized = null!;
    private CompiledQuery _fanoutThreeHashesBaseline = null!;
    private CompiledQuery _fanoutThreeHashesOptimized = null!;
    private CompiledQuery _stagedGraphMixedBaseline = null!;
    private CompiledQuery _stagedGraphMixedOptimized = null!;
    private CompiledQuery _semiKeySetBaseline = null!;
    private CompiledQuery _semiKeySetOptimized = null!;
    private CompiledQuery _antiKeySetBaseline = null!;
    private CompiledQuery _antiKeySetOptimized = null!;

    [Params(32, 10_000)] public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var data = CreateTestData(RowsCount);
        var schemaProvider = new CteBenchSchemaProvider(data);

        _singleHashBaseline = Compile(SingleHashBuildQuery, schemaProvider, BaselineOptions);
        _singleHashOptimized = Compile(SingleHashBuildQuery, schemaProvider, OptimizedOptions);
        _repeatedSelfJoinBaseline = Compile(RepeatedSelfJoinQuery, schemaProvider, BaselineOptions);
        _repeatedSelfJoinOptimized = Compile(RepeatedSelfJoinQuery, schemaProvider, OptimizedOptions);
        _multiConsumerBaseline = Compile(MultiConsumerSameKeyQuery, schemaProvider, BaselineOptions);
        _multiConsumerOptimized = Compile(MultiConsumerSameKeyQuery, schemaProvider, OptimizedOptions);
        _fanoutThreeHashesBaseline = Compile(FanoutThreeHashesQuery, schemaProvider, BaselineOptions);
        _fanoutThreeHashesOptimized = Compile(FanoutThreeHashesQuery, schemaProvider, OptimizedOptions);
        _stagedGraphMixedBaseline = Compile(StagedGraphMixedQuery, schemaProvider, BaselineOptions);
        _stagedGraphMixedOptimized = Compile(StagedGraphMixedQuery, schemaProvider, OptimizedOptions);
        _semiKeySetBaseline = Compile(SemiKeySetQuery, schemaProvider, BaselineOptions);
        _semiKeySetOptimized = Compile(SemiKeySetQuery, schemaProvider, OptimizedOptions);
        _antiKeySetBaseline = Compile(AntiKeySetQuery, schemaProvider, BaselineOptions);
        _antiKeySetOptimized = Compile(AntiKeySetQuery, schemaProvider, OptimizedOptions);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SingleHash")]
    public Table SingleHash_Baseline()
    {
        return _singleHashBaseline.Run();
    }

    [Benchmark]
    [BenchmarkCategory("SingleHash")]
    public Table SingleHash_Sidecar()
    {
        return _singleHashOptimized.Run();
    }

    [Benchmark]
    [BenchmarkCategory("RepeatedSelfJoin")]
    public Table RepeatedSelfJoin_Baseline()
    {
        return _repeatedSelfJoinBaseline.Run();
    }

    [Benchmark]
    [BenchmarkCategory("RepeatedSelfJoin")]
    public Table RepeatedSelfJoin_Sidecar()
    {
        return _repeatedSelfJoinOptimized.Run();
    }

    [Benchmark]
    [BenchmarkCategory("MultiConsumer")]
    public Table MultiConsumerSameKey_Baseline()
    {
        return _multiConsumerBaseline.Run();
    }

    [Benchmark]
    [BenchmarkCategory("MultiConsumer")]
    public Table MultiConsumerSameKey_Sidecar()
    {
        return _multiConsumerOptimized.Run();
    }

    [Benchmark]
    [BenchmarkCategory("Fanout")]
    public Table FanoutThreeHashes_Baseline()
    {
        return _fanoutThreeHashesBaseline.Run();
    }

    [Benchmark]
    [BenchmarkCategory("Fanout")]
    public Table FanoutThreeHashes_Sidecar()
    {
        return _fanoutThreeHashesOptimized.Run();
    }

    [Benchmark]
    [BenchmarkCategory("StagedGraph")]
    public Table StagedGraphMixed_Baseline()
    {
        return _stagedGraphMixedBaseline.Run();
    }

    [Benchmark]
    [BenchmarkCategory("StagedGraph")]
    public Table StagedGraphMixed_Sidecar()
    {
        return _stagedGraphMixedOptimized.Run();
    }

    [Benchmark]
    [BenchmarkCategory("KeySet")]
    public Table SemiKeySet_Baseline()
    {
        return _semiKeySetBaseline.Run();
    }

    [Benchmark]
    [BenchmarkCategory("KeySet")]
    public Table SemiKeySet_Sidecar()
    {
        return _semiKeySetOptimized.Run();
    }

    [Benchmark]
    [BenchmarkCategory("KeySet")]
    public Table AntiKeySet_Baseline()
    {
        return _antiKeySetBaseline.Run();
    }

    [Benchmark]
    [BenchmarkCategory("KeySet")]
    public Table AntiKeySet_Sidecar()
    {
        return _antiKeySetOptimized.Run();
    }

    private CompiledQuery Compile(
        string script,
        CteBenchSchemaProvider schemaProvider,
        CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            options);
    }

    private static List<CteBenchEntity> CreateTestData(int count)
    {
        var categories = new[] { "Alpha", "Beta", "Gamma", "Delta" };

        return Enumerable.Range(1, count)
            .Select(i => new CteBenchEntity
            {
                Id = i,
                Name = $"Entity_{i}",
                Value = i % 4,
                Category = categories[i % categories.Length]
            })
            .ToList();
    }

    private const string SingleHashBuildQuery = @"
with indexed as (
    select Id, Name, Category
    from #test.entities()
)
select l.Id, r.Name
from #test.entities() l
inner join indexed r on l.Id = r.Id";

    private const string RepeatedSelfJoinQuery = @"
with indexed as (
    select Id, Name, Category
    from #test.entities()
)
select l.Id, r.Name
from indexed l
inner join indexed r on l.Id = r.Id";

    private const string MultiConsumerSameKeyQuery = @"
with indexed as (
    select Id, Name, Category
    from #test.entities()
)
select l.Id, r.Name, s.Category
from #test.entities() l
inner join indexed r on l.Id = r.Id
inner join indexed s on l.Id = s.Id";

    private const string FanoutThreeHashesQuery = @"
with names as (
    select Id, Name
    from #test.entities()
),
categories as (
    select Id, Category
    from #test.entities()
),
scores as (
    select Id, Value
    from #test.entities()
)
select l.Id, n.Name, c.Category, s.Value
from #test.entities() l
inner join names n on l.Id = n.Id
inner join categories c on l.Id = c.Id
inner join scores s on l.Id = s.Id";

    private const string StagedGraphMixedQuery = @"
with raw as (
    select Id, Name, Category, Value
    from #test.entities()
),
names as (
    select Id, Name
    from raw
),
categories as (
    select Id, Category
    from raw
),
eligible as (
    select Id
    from raw
    where Value < 2
),
joined as (
    select l.Id, n.Name, c.Category
    from #test.entities() l
    inner join names n on l.Id = n.Id
    inner join categories c on l.Id = c.Id
)
select j.Id, j.Name, j.Category
from joined j
semi join eligible e on j.Id = e.Id";

    private const string SemiKeySetQuery = @"
with indexed as (
    select Id
    from #test.entities()
    where Value < 2
)
select l.Id
from #test.entities() l
semi join indexed r on l.Id = r.Id";

    private const string AntiKeySetQuery = @"
with indexed as (
    select Id
    from #test.entities()
    where Value < 2
)
select l.Id
from #test.entities() l
anti join indexed r on l.Id = r.Id";
}
