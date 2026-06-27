using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class SourcePlanningRuntimeOptimizationBenchmark
{
    private static readonly CompilationOptions Options = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(ParallelizationMode.None, useHashJoin: true, useSortMergeJoin: false));

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();

    private CompiledQuery _cardinalityDefault = null!;
    private CompiledQuery _cardinalityExact = null!;
    private CompiledQuery _predicateRuntimeOnly = null!;
    private CompiledQuery _predicateSourceAccepted = null!;

    [Params(10_000, 100_000)]
    public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var cardinalityRows = CreateAsymmetricJoinSources(RowsCount);
        var predicateRows = OptimizationBenchmarkRows.CreateSingleSource(RowsCount);

        _cardinalityDefault = Compile(
            CardinalityJoinQuery,
            cardinalityRows,
            OptimizationBenchmarkPlanningMode.RejectAll);
        _cardinalityExact = Compile(
            CardinalityJoinQuery,
            cardinalityRows,
            OptimizationBenchmarkPlanningMode.RejectAllWithExactCardinality);
        _predicateRuntimeOnly = Compile(
            PredicateResidualQuery,
            predicateRows,
            OptimizationBenchmarkPlanningMode.RejectAll);
        _predicateSourceAccepted = Compile(
            PredicateResidualQuery,
            predicateRows,
            OptimizationBenchmarkPlanningMode.AcceptPredicate);
    }

    [Benchmark(Baseline = true)]
    public Table CardinalityHashBuild_Default()
    {
        return _cardinalityDefault.Run();
    }

    [Benchmark]
    public Table CardinalityHashBuild_SourceExactRows()
    {
        return _cardinalityExact.Run();
    }

    [Benchmark]
    public Table PredicateResidual_RuntimeOnly()
    {
        return _predicateRuntimeOnly.Run();
    }

    [Benchmark]
    public Table PredicateResidual_SourceAccepted()
    {
        return _predicateSourceAccepted.Run();
    }

    private CompiledQuery Compile(
        string query,
        IReadOnlyDictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> rowsBySchema,
        OptimizationBenchmarkPlanningMode mode)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new OptimizationBenchmarkSchemaProvider(rowsBySchema, mode),
            _loggerResolver,
            Options);
    }

    private static Dictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> CreateAsymmetricJoinSources(
        int largeCount)
    {
        var large = OptimizationBenchmarkRows.Create(largeCount);
        var small = large.Take(Math.Max(1, largeCount / 100)).ToArray();

        return new Dictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["#left"] = small,
            ["#right"] = large
        };
    }

    private const string CardinalityJoinQuery = @"
        select l.Id, r.Id, l.Score, r.Score
        from #left.items() l
        inner join #right.items() r on l.JoinKey = r.JoinKey";

    private const string PredicateResidualQuery = @"
        select b.Id, b.Category, b.Score
        from #bench.items() b
        where b.Category = 'hot-a'
          and b.Score > 100
          and b.Score + 1 > 0";
}
