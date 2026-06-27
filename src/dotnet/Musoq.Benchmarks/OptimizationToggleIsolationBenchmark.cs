using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class OptimizationToggleIsolationBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();

    private CompiledQuery _constantFoldingOff = null!;
    private CompiledQuery _constantFoldingOn = null!;
    private CompiledQuery _cseOff = null!;
    private CompiledQuery _cseOn = null!;
    private CompiledQuery _cteParallelizationOff = null!;
    private CompiledQuery _cteParallelizationOn = null!;
    private CompiledQuery _hashJoinOff = null!;
    private CompiledQuery _hashJoinOn = null!;
    private CompiledQuery _parallelizationFull = null!;
    private CompiledQuery _parallelizationNone = null!;
    private CompiledQuery _sortMergeJoinOff = null!;
    private CompiledQuery _sortMergeJoinOn = null!;

    [Params(10_000, 100_000)]
    public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var singleSourceRows = OptimizationBenchmarkRows.CreateSingleSource(RowsCount);
        var joinRows = OptimizationBenchmarkRows.CreateJoinSources(RowsCount);

        _hashJoinOn = Compile(
            JoinQuery,
            joinRows,
            new CompilationOptions(ParallelizationMode.None, useHashJoin: true, useSortMergeJoin: false));
        _hashJoinOff = Compile(
            JoinQuery,
            joinRows,
            new CompilationOptions(ParallelizationMode.None, useHashJoin: false, useSortMergeJoin: false));

        _sortMergeJoinOn = Compile(
            JoinQuery,
            joinRows,
            new CompilationOptions(ParallelizationMode.None, useHashJoin: false, useSortMergeJoin: true));
        _sortMergeJoinOff = Compile(
            JoinQuery,
            joinRows,
            new CompilationOptions(ParallelizationMode.None, useHashJoin: false, useSortMergeJoin: false));

        _cseOn = Compile(
            RepeatedExpressionQuery,
            singleSourceRows,
            new CompilationOptions(ParallelizationMode.None, useCommonSubexpressionElimination: true));
        _cseOff = Compile(
            RepeatedExpressionQuery,
            singleSourceRows,
            new CompilationOptions(ParallelizationMode.None, useCommonSubexpressionElimination: false));

        _constantFoldingOn = Compile(
            ConstantFoldingQuery,
            singleSourceRows,
            new CompilationOptions(ParallelizationMode.None, useConstantFolding: true));
        _constantFoldingOff = Compile(
            ConstantFoldingQuery,
            singleSourceRows,
            new CompilationOptions(ParallelizationMode.None, useConstantFolding: false));

        _parallelizationFull = Compile(
            ParallelFilterProjectQuery,
            singleSourceRows,
            new CompilationOptions(ParallelizationMode.Full));
        _parallelizationNone = Compile(
            ParallelFilterProjectQuery,
            singleSourceRows,
            new CompilationOptions(ParallelizationMode.None));

        _cteParallelizationOn = Compile(
            IndependentCteQuery,
            joinRows,
            new CompilationOptions(ParallelizationMode.None, useCteParallelization: true));
        _cteParallelizationOff = Compile(
            IndependentCteQuery,
            joinRows,
            new CompilationOptions(ParallelizationMode.None, useCteParallelization: false));
    }

    [Benchmark(Baseline = true)]
    public Table HashJoin_On()
    {
        return _hashJoinOn.Run();
    }

    [Benchmark]
    public Table HashJoin_Off()
    {
        return _hashJoinOff.Run();
    }

    [Benchmark]
    public Table SortMergeJoin_On()
    {
        return _sortMergeJoinOn.Run();
    }

    [Benchmark]
    public Table SortMergeJoin_Off()
    {
        return _sortMergeJoinOff.Run();
    }

    [Benchmark]
    public Table CommonSubexpressionElimination_On()
    {
        return _cseOn.Run();
    }

    [Benchmark]
    public Table CommonSubexpressionElimination_Off()
    {
        return _cseOff.Run();
    }

    [Benchmark]
    public Table ConstantFolding_On()
    {
        return _constantFoldingOn.Run();
    }

    [Benchmark]
    public Table ConstantFolding_Off()
    {
        return _constantFoldingOff.Run();
    }

    [Benchmark]
    public Table Parallelization_Full()
    {
        return _parallelizationFull.Run();
    }

    [Benchmark]
    public Table Parallelization_None()
    {
        return _parallelizationNone.Run();
    }

    [Benchmark]
    public Table CteParallelization_On()
    {
        return _cteParallelizationOn.Run();
    }

    [Benchmark]
    public Table CteParallelization_Off()
    {
        return _cteParallelizationOff.Run();
    }

    private CompiledQuery Compile(
        string query,
        IReadOnlyDictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> rowsBySchema,
        CompilationOptions options)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new OptimizationBenchmarkSchemaProvider(rowsBySchema),
            _loggerResolver,
            BenchmarkCompilationOptions.Materialized(options));
    }

    private const string JoinQuery = @"
        select l.Id, r.Id, l.Score, r.Score
        from #left.items() l
        inner join #right.items() r on l.JoinKey = r.JoinKey";

    private const string RepeatedExpressionQuery = @"
        select
            b.Id,
            ExpensiveCompute(b.Value),
            ExpensiveCompute(b.Value) + 10,
            ExpensiveCompute(b.Value) * 2
        from #bench.items() b
        where ExpensiveCompute(b.Value) > 100";

    private const string ConstantFoldingQuery = @"
        select
            b.Id,
            b.Value + (10 * 20) + (30 / 3)
        from #bench.items() b
        where b.Value > (100 + 50)
          and (2 + 2) = 4";

    private const string ParallelFilterProjectQuery = @"
        select b.Id, b.Name, ExpensiveCompute(b.Value)
        from #bench.items() b
        where b.Value > 100
          and b.Score > 50";

    private const string IndependentCteQuery = @"
        with leftFiltered as (
            select Id, JoinKey, Score
            from #left.items() l
            where l.Score > 100
        ),
        rightFiltered as (
            select Id, JoinKey, Score
            from #right.items() r
            where r.Score > 100
        )
        select l.Id, r.Id
        from leftFiltered l
        inner join rightFiltered r on l.JoinKey = r.JoinKey";
}
