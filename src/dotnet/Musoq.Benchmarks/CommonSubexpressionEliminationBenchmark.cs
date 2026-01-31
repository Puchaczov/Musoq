using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmark to measure the impact of Common Subexpression Elimination (CSE).
///     Compares queries with duplicate expressions vs queries without.
///     CSE should reduce execution time by caching computed values that are used multiple times
///     in the same row context (e.g., same expression in WHERE and SELECT).
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class CommonSubexpressionEliminationBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _queryCaseWhenNoDuplicate = null!;
    private CompiledQuery _queryCaseWhenWithDuplicateInSelect = null!;
    private CompiledQuery _queryCaseWhenWithDuplicateInWhere = null!;
    private CompiledQuery _queryWithDuplicateExpressions = null!;
    private CompiledQuery _queryWithNestedDuplicates = null!;
    private CompiledQuery _queryWithoutDuplicateExpressions = null!;
    private CompiledQuery _queryWithTripleDuplicates = null!;

    [Params(10_000, 100_000)] public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var testData = CreateTestData(RowsCount);
        var schemaProvider = new CseTestSchemaProvider(testData);


        _queryWithDuplicateExpressions = InstanceCreator.CompileForExecution(
            @"SELECT ExpensiveMethod(Value), Name
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryWithoutDuplicateExpressions = InstanceCreator.CompileForExecution(
            @"SELECT Value * 2, Name
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryWithTripleDuplicates = InstanceCreator.CompileForExecution(
            @"SELECT ExpensiveMethod(Value), ExpensiveMethod(Value) + 10, Name
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryWithNestedDuplicates = InstanceCreator.CompileForExecution(
            @"SELECT ExpensiveMethod(Value) * 2, ExpensiveMethod(Value) / 2
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 50 AND ExpensiveMethod(Value) < 1000",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryCaseWhenWithDuplicateInSelect = InstanceCreator.CompileForExecution(
            @"SELECT ExpensiveMethod(Value),
                     CASE WHEN ExpensiveMethod(Value) > 200 THEN 'High' ELSE 'Low' END
              FROM #test.entities()",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryCaseWhenWithDuplicateInWhere = InstanceCreator.CompileForExecution(
            @"SELECT Name,
                     CASE WHEN ExpensiveMethod(Value) > 200 THEN 'High' ELSE 'Low' END
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 100",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);


        _queryCaseWhenNoDuplicate = InstanceCreator.CompileForExecution(
            @"SELECT Name,
                     CASE WHEN ExpensiveMethod(Value) > 200 THEN 'High' ELSE 'Low' END
              FROM #test.entities()",
            Guid.NewGuid().ToString(),
            schemaProvider,
            _loggerResolver);
    }

    /// <summary>
    ///     Baseline: No duplicate expressions.
    ///     ExpensiveMethod called once per row.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void Query_NoDuplicates()
    {
        _queryWithoutDuplicateExpressions.Run();
    }

    /// <summary>
    ///     Same expression in WHERE and SELECT (2x calls per row).
    ///     With CSE, should approach baseline performance.
    /// </summary>
    [Benchmark]
    public void Query_DuplicateInWhereAndSelect()
    {
        _queryWithDuplicateExpressions.Run();
    }

    /// <summary>
    ///     Same expression 3 times (WHERE + 2x in SELECT).
    ///     Without CSE: 3x the computation.
    ///     With CSE: Should be ~same as baseline.
    /// </summary>
    [Benchmark]
    public void Query_TripleDuplicates()
    {
        _queryWithTripleDuplicates.Run();
    }

    /// <summary>
    ///     Expression appears 4 times in different contexts.
    ///     Tests that CSE handles complex scenarios.
    /// </summary>
    [Benchmark]
    public void Query_NestedDuplicates()
    {
        _queryWithNestedDuplicates.Run();
    }

    /// <summary>
    ///     CASE WHEN baseline: ExpensiveMethod only inside CASE WHEN.
    ///     No CSE benefit expected - expression doesn't appear outside CASE WHEN.
    /// </summary>
    [Benchmark]
    public void Query_CaseWhen_NoDuplicate()
    {
        _queryCaseWhenNoDuplicate.Run();
    }

    /// <summary>
    ///     CASE WHEN with duplicate: ExpensiveMethod in SELECT and inside CASE WHEN.
    ///     With CSE: cached value passed to CaseWhen method as parameter.
    ///     Should be faster than NoDuplicate since expression is cached.
    /// </summary>
    [Benchmark]
    public void Query_CaseWhen_DuplicateInSelect()
    {
        _queryCaseWhenWithDuplicateInSelect.Run();
    }

    /// <summary>
    ///     CASE WHEN with duplicate: ExpensiveMethod in WHERE and inside CASE WHEN.
    ///     With CSE: cached value passed to CaseWhen method as parameter.
    /// </summary>
    [Benchmark]
    public void Query_CaseWhen_DuplicateInWhere()
    {
        _queryCaseWhenWithDuplicateInWhere.Run();
    }

    private static List<CseTestEntity> CreateTestData(int count)
    {
        return Enumerable.Range(0, count).Select(i => new CseTestEntity
        {
            Id = i,
            Name = $"Name{i}",
            Value = i % 500,
            Category = $"Category{i % 10}"
        }).ToList();
    }
}

// Note: SchemaColumn is defined in RegexPluginBenchmark.cs - reusing it here
