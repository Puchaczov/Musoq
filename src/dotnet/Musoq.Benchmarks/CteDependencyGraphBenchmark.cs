using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmark for CTE dependency graph optimizations.
///     Implemented optimizations:
///     - Dead CTE elimination: Removes CTEs that are not reachable from the outer query (ALWAYS ON)
///     - CTE parallelization: Independent CTEs at the same execution level run in parallel
///     These benchmarks demonstrate:
///     1. Dead CTE elimination savings
///     2. Parallel CTE execution benefits
///     Expected Results Analysis:
///     - NoCtes_Baseline: Pure direct query, no CTE overhead
///     - SingleDeadCte_Eliminated: 1 used CTE + 1 dead CTE (eliminated) ≈ overhead of 1 CTE
///     - MultipleDeadCtes_Eliminated: 1 used CTE + 3 dead CTEs (eliminated) ≈ overhead of 1 CTE
///     - Parallel vs Sequential: Multiple independent CTEs should be faster in parallel
///     Key metrics to validate:
///     1. MultipleDeadCtes_Eliminated should be MUCH faster than MultipleUsedCtes_AllComputed
///     2. Parallel execution should be faster than sequential for independent CTEs
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class CteDependencyGraphBenchmark
{
    private static readonly CompilationOptions SequentialOptions = new(
        ParallelizationMode.Full,
        true,
        true,
        true,
        true,
        false);

    private static readonly CompilationOptions ParallelOptions = new(
        ParallelizationMode.Full,
        true,
        true,
        true,
        true,
        true);

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _eightIndependentCtes_Parallel = null!;
    private CompiledQuery _eightIndependentCtes_Sequential = null!;
    private CompiledQuery _fourExpensiveCtes_Parallel = null!;

    // Parallelization benchmarks - EXPENSIVE CTEs (simulated heavy work - WILL show speedup)
    private CompiledQuery _fourExpensiveCtes_Sequential = null!;
    private CompiledQuery _fourIndependentCtes_Parallel = null!;

    // Parallelization benchmarks - simple CTEs (trivial work - may not show speedup)
    private CompiledQuery _fourIndependentCtes_Sequential = null!;
    private CompiledQuery _multipleDeadCtes = null!; // 3 dead CTEs eliminated
    private CompiledQuery _multipleUsedCtes = null!; // Same CTEs but all used (baseline)
    private CompiledQuery _noCtes = null!; // No CTEs at all (cleanest baseline)

    // Dead CTE benchmarks - comparing eliminated vs used
    private CompiledQuery _singleDeadCte = null!; // 1 dead CTE eliminated
    private CompiledQuery _singleUsedCte = null!; // Same CTE but actually used (baseline for comparison)

    [Params(1_000, 10_000)] public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var testData = CreateTestData(RowsCount);
        var schemaProvider = new CteBenchSchemaProvider(testData);


        var expensiveSchemaProvider = new CteBenchSchemaProvider(testData, 10_000_000);

        SetupDeadCteEliminationBenchmarks(schemaProvider);
        SetupParallelizationBenchmarks(schemaProvider);
        SetupExpensiveParallelizationBenchmarks(expensiveSchemaProvider);
    }

    #region Test Data Creation

    private static List<CteBenchEntity> CreateTestData(int count)
    {
        var random = new Random(42);
        var categories = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };

        return Enumerable.Range(1, count)
            .Select(i => new CteBenchEntity
            {
                Id = i,
                Name = $"Entity_{i}",
                Value = random.Next(1, 1001),
                Category = categories[i % categories.Length]
            })
            .ToList();
    }

    #endregion

    #region Setup Methods

    private void SetupDeadCteEliminationBenchmarks(CteBenchSchemaProvider schemaProvider)
    {
        const string noCtesQuery = @"
            select Id, Name, Abs(Value) * 2 + 1 ComputedValue
            from #test.entities()
            where Value > 500";

        _noCtes = InstanceCreator.CompileForExecution(
            noCtesQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            SequentialOptions);


        const string singleDeadCteQuery = @"
            with cteUsed as (
                select Id, Name, Abs(Value) * 2 + 1 ComputedValue
                from #test.entities()
                where Value > 500
            ),
            cteUnused as (
                select Id, Name, Abs(Value) * 3 + 2 UnusedValue
                from #test.entities()
                where Value <= 500
            )
            select Id, Name, ComputedValue from cteUsed";

        _singleDeadCte = InstanceCreator.CompileForExecution(
            singleDeadCteQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            SequentialOptions);


        const string singleUsedCteQuery = @"
            with cteUsed as (
                select Id, Name, Abs(Value) * 2 + 1 ComputedValue
                from #test.entities()
                where Value > 500
            ),
            cteOther as (
                select Id, Name, Abs(Value) * 3 + 2 OtherValue
                from #test.entities()
                where Value <= 500
            )
            select u.Id, u.Name, u.ComputedValue, o.OtherValue
            from cteUsed u inner join cteOther o on u.Id = o.Id + 1";

        _singleUsedCte = InstanceCreator.CompileForExecution(
            singleUsedCteQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            SequentialOptions);


        const string multipleDeadCtesQuery = @"
            with cteUsed as (
                select Id, Name, Abs(Value) * 2 + 1 ComputedValue
                from #test.entities()
                where Value > 500
            ),
            cteDead1 as (
                select Id, Name, Abs(Value) * 3 DeadValue1
                from #test.entities()
                where Value > 250 and Value <= 500
            ),
            cteDead2 as (
                select Id, Name, Abs(Value) + 100 DeadValue2
                from #test.entities()
                where Value <= 250
            ),
            cteDead3 as (
                select Id, Name, Abs(Value) * Value + Value DeadValue3
                from #test.entities()
            )
            select Id, Name, ComputedValue from cteUsed";

        _multipleDeadCtes = InstanceCreator.CompileForExecution(
            multipleDeadCtesQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            SequentialOptions);


        const string multipleUsedCtesQuery = @"
            with cteA as (
                select Id, Name, Abs(Value) * 2 + 1 ValueA
                from #test.entities()
                where Value > 500
            ),
            cteB as (
                select Id, Name, Abs(Value) * 3 ValueB
                from #test.entities()
                where Value > 250 and Value <= 500
            ),
            cteC as (
                select Id, Name, Abs(Value) + 100 ValueC
                from #test.entities()
                where Value <= 250
            ),
            cteD as (
                select Id, Name, Abs(Value) * Value + Value ValueD
                from #test.entities()
            )
            select a.Id, a.Name, a.ValueA, b.ValueB, c.ValueC, d.ValueD
            from cteA a
            inner join cteB b on a.Id = b.Id + 1
            inner join cteC c on b.Id = c.Id + 1
            inner join cteD d on c.Id = d.Id + 1";

        _multipleUsedCtes = InstanceCreator.CompileForExecution(
            multipleUsedCtesQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            SequentialOptions);
    }

    private void SetupParallelizationBenchmarks(CteBenchSchemaProvider schemaProvider)
    {
        const string fourIndependentCtesQuery = @"
            with cte1 as (
                select Id, Name, Abs(Value) * 2 + 1 as V1 from #test.entities()
            ),
            cte2 as (
                select Id, Name, Abs(Value) * 3 + 2 as V2 from #test.entities()
            ),
            cte3 as (
                select Id, Name, Abs(Value) * 5 + 3 as V3 from #test.entities()
            ),
            cte4 as (
                select Id, Name, Abs(Value) * 7 + 4 as V4 from #test.entities()
            )
            select a.Id, a.V1, b.V2, c.V3, d.V4
            from cte1 a
            inner join cte2 b on a.Id = b.Id
            inner join cte3 c on a.Id = c.Id
            inner join cte4 d on a.Id = d.Id";

        _fourIndependentCtes_Sequential = InstanceCreator.CompileForExecution(
            fourIndependentCtesQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            SequentialOptions);

        _fourIndependentCtes_Parallel = InstanceCreator.CompileForExecution(
            fourIndependentCtesQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            ParallelOptions);


        const string eightIndependentCtesQuery = @"
            with cte1 as (
                select Id, Name, Abs(Value) * 2 + 1 as V1 from #test.entities()
            ),
            cte2 as (
                select Id, Name, Abs(Value) * 3 + 2 as V2 from #test.entities()
            ),
            cte3 as (
                select Id, Name, Abs(Value) * 5 + 3 as V3 from #test.entities()
            ),
            cte4 as (
                select Id, Name, Abs(Value) * 7 + 4 as V4 from #test.entities()
            ),
            cte5 as (
                select Id, Name, Abs(Value) * 11 + 5 as V5 from #test.entities()
            ),
            cte6 as (
                select Id, Name, Abs(Value) * 13 + 6 as V6 from #test.entities()
            ),
            cte7 as (
                select Id, Name, Abs(Value) * 17 + 7 as V7 from #test.entities()
            ),
            cte8 as (
                select Id, Name, Abs(Value) * 19 + 8 as V8 from #test.entities()
            )
            select a.Id, a.V1, b.V2, c.V3, d.V4, e.V5, f.V6, g.V7, h.V8
            from cte1 a
            inner join cte2 b on a.Id = b.Id
            inner join cte3 c on a.Id = c.Id
            inner join cte4 d on a.Id = d.Id
            inner join cte5 e on a.Id = e.Id
            inner join cte6 f on a.Id = f.Id
            inner join cte7 g on a.Id = g.Id
            inner join cte8 h on a.Id = h.Id";

        _eightIndependentCtes_Sequential = InstanceCreator.CompileForExecution(
            eightIndependentCtesQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            SequentialOptions);

        _eightIndependentCtes_Parallel = InstanceCreator.CompileForExecution(
            eightIndependentCtesQuery,
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver,
            ParallelOptions);
    }

    private void SetupExpensiveParallelizationBenchmarks(CteBenchSchemaProvider expensiveSchemaProvider)
    {
        const string fourExpensiveCtesQuery = @"
            with cte1 as (
                select Id, Name, Abs(Value) * 2 + 1 as V1 from #test.entities()
            ),
            cte2 as (
                select Id, Name, Abs(Value) * 3 + 2 as V2 from #test.entities()
            ),
            cte3 as (
                select Id, Name, Abs(Value) * 5 + 3 as V3 from #test.entities()
            ),
            cte4 as (
                select Id, Name, Abs(Value) * 7 + 4 as V4 from #test.entities()
            )
            select a.Id, a.V1, b.V2, c.V3, d.V4
            from cte1 a
            inner join cte2 b on a.Id = b.Id
            inner join cte3 c on a.Id = c.Id
            inner join cte4 d on a.Id = d.Id";

        _fourExpensiveCtes_Sequential = InstanceCreator.CompileForExecution(
            fourExpensiveCtesQuery,
            Guid.NewGuid().ToString(),
            expensiveSchemaProvider,
            _loggerResolver,
            SequentialOptions);

        _fourExpensiveCtes_Parallel = InstanceCreator.CompileForExecution(
            fourExpensiveCtesQuery,
            Guid.NewGuid().ToString(),
            expensiveSchemaProvider,
            _loggerResolver,
            ParallelOptions);
    }

    #endregion

    #region Dead CTE Elimination Benchmarks

    /// <summary>
    ///     Baseline: Query without any CTEs.
    ///     This is the cleanest reference point.
    /// </summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DeadCTE")]
    public object NoCtes_Baseline()
    {
        return _noCtes.Run();
    }

    /// <summary>
    ///     Query with 1 dead CTE that gets eliminated.
    ///     Should be nearly as fast as NoCtes_Baseline since the dead CTE is never computed.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("DeadCTE")]
    public object SingleDeadCte_Eliminated()
    {
        return _singleDeadCte.Run();
    }

    /// <summary>
    ///     Query with 2 CTEs where both are used (joined).
    ///     This is the "without elimination" baseline - shows the work saved.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("DeadCTE")]
    public object SingleUsedCte_AllComputed()
    {
        return _singleUsedCte.Run();
    }

    /// <summary>
    ///     Query with 4 CTEs where 3 are dead and eliminated.
    ///     Should be much faster than MultipleUsedCtes since 3 CTEs are never computed.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("DeadCTE")]
    public object MultipleDeadCtes_Eliminated()
    {
        return _multipleDeadCtes.Run();
    }

    /// <summary>
    ///     Query with 4 CTEs where all are used (joined).
    ///     This shows what would happen without dead CTE elimination.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("DeadCTE")]
    public object MultipleUsedCtes_AllComputed()
    {
        return _multipleUsedCtes.Run();
    }

    #endregion

    #region Parallelization Benchmarks

    /// <summary>
    ///     4 independent CTEs executed sequentially.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Parallel")]
    public object FourIndependentCtes_Sequential()
    {
        return _fourIndependentCtes_Sequential.Run();
    }

    /// <summary>
    ///     4 independent CTEs executed in parallel.
    ///     Should be significantly faster than sequential on multi-core systems.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Parallel")]
    public object FourIndependentCtes_Parallel()
    {
        return _fourIndependentCtes_Parallel.Run();
    }

    /// <summary>
    ///     8 independent CTEs executed sequentially.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Parallel")]
    public object EightIndependentCtes_Sequential()
    {
        return _eightIndependentCtes_Sequential.Run();
    }

    /// <summary>
    ///     8 independent CTEs executed in parallel.
    ///     Should show even more benefit on systems with many cores.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Parallel")]
    public object EightIndependentCtes_Parallel()
    {
        return _eightIndependentCtes_Parallel.Run();
    }

    #endregion

    #region Expensive CTE Parallelization Benchmarks

    /// <summary>
    ///     4 EXPENSIVE independent CTEs executed sequentially.
    ///     Each CTE triggers ~100ms of simulated work, so total ~400ms for CTE phase.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("ExpensiveParallel")]
    public object FourExpensiveCtes_Sequential()
    {
        return _fourExpensiveCtes_Sequential.Run();
    }

    /// <summary>
    ///     4 EXPENSIVE independent CTEs executed in parallel.
    ///     Should be ~4x faster than sequential for the CTE phase (all 4 CTEs run simultaneously).
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("ExpensiveParallel")]
    public object FourExpensiveCtes_Parallel()
    {
        return _fourExpensiveCtes_Parallel.Run();
    }

    #endregion
}

#region Schema Components

#endregion
