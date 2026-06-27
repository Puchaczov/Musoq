using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class SourcePlanningV1Benchmark
{
    private static readonly CompilationOptions Options = BenchmarkCompilationOptions.Materialized(
        new CompilationOptions(ParallelizationMode.None));

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();

    private CompiledQuery _globalJoinOrderTakeOff = null!;
    private CompiledQuery _globalJoinOrderTakeOn = null!;
    private CompiledQuery _orderNatural = null!;
    private CompiledQuery _orderNaive = null!;
    private CompiledQuery _orderRuntime = null!;
    private CompiledQuery _orderSkipTakeNatural = null!;
    private CompiledQuery _orderSkipTakeNaive = null!;
    private CompiledQuery _orderSkipTakeRuntime = null!;
    private CompiledQuery _orderSkipTakeTopN = null!;
    private CompiledQuery _orderTakeNatural = null!;
    private CompiledQuery _orderTakeNaive = null!;
    private CompiledQuery _orderTakeRuntime = null!;
    private CompiledQuery _orderTakeTopN = null!;
    private CompiledQuery _skipTakeOff = null!;
    private CompiledQuery _skipTakeOn = null!;
    private CompiledQuery _takeOff = null!;
    private CompiledQuery _takeOn = null!;

    [Params(10_000, 100_000)]
    public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = OptimizationBenchmarkRows.Create(RowsCount);
        var singleSourceRows = OptimizationBenchmarkRows.CreateSingleSource(rows);
        var scoreNaturalRows = OptimizationBenchmarkRows.CreateSingleSource(
            rows.OrderByDescending(static row => row.Score).ThenBy(static row => row.Id).ToArray());
        var categoryScoreNaturalRows = OptimizationBenchmarkRows.CreateSingleSource(
            rows.OrderBy(static row => row.Category, StringComparer.Ordinal)
                .ThenByDescending(static row => row.Score)
                .ThenBy(static row => row.Id)
                .ToArray());
        var joinRows = OptimizationBenchmarkRows.CreateJoinSources(RowsCount);

        _takeOff = Compile(TakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.RejectAll);
        _takeOn = Compile(TakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.AcceptTake);

        _skipTakeOff = Compile(SkipTakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.RejectAll);
        _skipTakeOn = Compile(SkipTakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.AcceptSkipTake);

        _orderRuntime = Compile(OrderQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.RejectAll);
        _orderNaive = Compile(OrderQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.AcceptNaiveOrder);
        _orderNatural = Compile(OrderQuery, scoreNaturalRows, OptimizationBenchmarkPlanningMode.AcceptNaturalOrder);

        _orderTakeRuntime = Compile(OrderTakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.RejectAll);
        _orderTakeNaive = Compile(OrderTakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.AcceptNaiveOrderSkipTake);
        _orderTakeTopN = Compile(OrderTakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.AcceptTopNOrderSkipTake);
        _orderTakeNatural = Compile(OrderTakeQuery, scoreNaturalRows, OptimizationBenchmarkPlanningMode.AcceptNaturalOrderSkipTake);

        _orderSkipTakeRuntime = Compile(OrderSkipTakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.RejectAll);
        _orderSkipTakeNaive = Compile(OrderSkipTakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.AcceptNaiveOrderSkipTake);
        _orderSkipTakeTopN = Compile(OrderSkipTakeQuery, singleSourceRows, OptimizationBenchmarkPlanningMode.AcceptTopNOrderSkipTake);
        _orderSkipTakeNatural = Compile(OrderSkipTakeQuery, categoryScoreNaturalRows, OptimizationBenchmarkPlanningMode.AcceptNaturalOrderSkipTake);

        _globalJoinOrderTakeOff = Compile(GlobalJoinOrderTakeQuery, joinRows, OptimizationBenchmarkPlanningMode.RejectAll);
        _globalJoinOrderTakeOn = Compile(GlobalJoinOrderTakeQuery, joinRows, OptimizationBenchmarkPlanningMode.AcceptOrderSkipTake);
    }

    [Benchmark(Baseline = true)]
    public Table Take_SourcePlanningOff()
    {
        return _takeOff.Run();
    }

    [Benchmark]
    public Table Take_SourcePlanningOn()
    {
        return _takeOn.Run();
    }

    [Benchmark]
    public Table SkipTake_SourcePlanningOff()
    {
        return _skipTakeOff.Run();
    }

    [Benchmark]
    public Table SkipTake_SourcePlanningOn()
    {
        return _skipTakeOn.Run();
    }

    [Benchmark]
    public Table Order_RuntimeSort()
    {
        return _orderRuntime.Run();
    }

    [Benchmark]
    public Table Order_NaiveSourceSort()
    {
        return _orderNaive.Run();
    }

    [Benchmark]
    public Table Order_NaturalSourceOrder()
    {
        return _orderNatural.Run();
    }

    [Benchmark]
    public Table OrderTake_RuntimeTopN()
    {
        return _orderTakeRuntime.Run();
    }

    [Benchmark]
    public Table OrderTake_NaiveSourceSort()
    {
        return _orderTakeNaive.Run();
    }

    [Benchmark]
    public Table OrderTake_TopNSource()
    {
        return _orderTakeTopN.Run();
    }

    [Benchmark]
    public Table OrderTake_NaturalSourceOrder()
    {
        return _orderTakeNatural.Run();
    }

    [Benchmark]
    public Table OrderSkipTake_RuntimeTopOffset()
    {
        return _orderSkipTakeRuntime.Run();
    }

    [Benchmark]
    public Table OrderSkipTake_NaiveSourceSort()
    {
        return _orderSkipTakeNaive.Run();
    }

    [Benchmark]
    public Table OrderSkipTake_TopNSource()
    {
        return _orderSkipTakeTopN.Run();
    }

    [Benchmark]
    public Table OrderSkipTake_NaturalSourceOrder()
    {
        return _orderSkipTakeNatural.Run();
    }

    [Benchmark]
    public Table GlobalJoinOrderTake_SourcePlanningOff()
    {
        return _globalJoinOrderTakeOff.Run();
    }

    [Benchmark]
    public Table GlobalJoinOrderTake_SourcePlanningOn()
    {
        return _globalJoinOrderTakeOn.Run();
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

    private const string TakeQuery =
        "select b.Id, b.Name, b.Score from #bench.items() b take 100";

    private const string SkipTakeQuery =
        "select b.Id, b.Name, b.Score from #bench.items() b skip 1000 take 100";

    private const string OrderQuery =
        "select b.Id, b.Name, b.Score from #bench.items() b order by b.Score desc";

    private const string OrderTakeQuery =
        "select b.Id, b.Name, b.Score from #bench.items() b order by b.Score desc take 100";

    private const string OrderSkipTakeQuery =
        "select b.Id, b.Name, b.Score from #bench.items() b order by b.Category, b.Score desc skip 1000 take 100";

    private const string GlobalJoinOrderTakeQuery = @"
        select l.Id, r.Name, l.Score
        from #left.items() l
        inner join #right.items() r on l.JoinKey = r.JoinKey
        order by l.Score desc
        take 100";
}
