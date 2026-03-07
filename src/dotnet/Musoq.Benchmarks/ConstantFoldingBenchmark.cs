using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmark to measure the impact of constant folding optimization.
///     Compares query execution with constant folding enabled vs disabled.
///     Constant folding pre-evaluates constant expressions at compile time,
///     reducing per-row arithmetic overhead at runtime.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class ConstantFoldingBenchmark
{
    private static readonly CompilationOptions FoldingEnabled = new(
        ParallelizationMode.Full,
        useConstantFolding: true);

    private static readonly CompilationOptions FoldingDisabled = new(
        ParallelizationMode.Full,
        useConstantFolding: false);

    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private CompiledQuery _arithmeticFoldingDisabled = null!;
    private CompiledQuery _arithmeticFoldingEnabled = null!;
    private CompiledQuery _heavyConstantsFoldingDisabled = null!;
    private CompiledQuery _heavyConstantsFoldingEnabled = null!;
    private CompiledQuery _mixedColumnConstantFoldingDisabled = null!;
    private CompiledQuery _mixedColumnConstantFoldingEnabled = null!;
    private CompiledQuery _stringConcatFoldingDisabled = null!;
    private CompiledQuery _stringConcatFoldingEnabled = null!;

    [Params(10_000, 100_000)] public int RowsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var testData = CreateTestData(RowsCount);
        var schemaProvider = new OptBenchSchemaProvider(testData);

        // Query 1: basic constant arithmetic in select
        const string arithmeticQuery = @"
            SELECT Value + (10 + 20 + 30), Name
            FROM #test.entities()
            WHERE Value > (100 - 50)";

        _arithmeticFoldingEnabled = InstanceCreator.CompileForExecution(
            arithmeticQuery, Guid.NewGuid().ToString(),
            schemaProvider, _loggerResolver, FoldingEnabled);

        _arithmeticFoldingDisabled = InstanceCreator.CompileForExecution(
            arithmeticQuery, Guid.NewGuid().ToString(),
            schemaProvider, _loggerResolver, FoldingDisabled);

        // Query 2: heavy constant expressions (many operations per row)
        const string heavyConstantsQuery = @"
            SELECT
                Value + (1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10),
                Value * (2 * 3 * 4),
                Value - (100 - 50 - 25)
            FROM #test.entities()
            WHERE Value > (10 * 10) AND Value < (50 * 20)";

        _heavyConstantsFoldingEnabled = InstanceCreator.CompileForExecution(
            heavyConstantsQuery, Guid.NewGuid().ToString(),
            schemaProvider, _loggerResolver, FoldingEnabled);

        _heavyConstantsFoldingDisabled = InstanceCreator.CompileForExecution(
            heavyConstantsQuery, Guid.NewGuid().ToString(),
            schemaProvider, _loggerResolver, FoldingDisabled);

        // Query 3: string constant concatenation with adjacent folding
        const string stringConcatQuery = @"
            SELECT
                'prefix_' + Name + '_mid' + '_end',
                'key:' + Category + ':val' + ':done'
            FROM #test.entities()";

        _stringConcatFoldingEnabled = InstanceCreator.CompileForExecution(
            stringConcatQuery, Guid.NewGuid().ToString(),
            schemaProvider, _loggerResolver, FoldingEnabled);

        _stringConcatFoldingDisabled = InstanceCreator.CompileForExecution(
            stringConcatQuery, Guid.NewGuid().ToString(),
            schemaProvider, _loggerResolver, FoldingDisabled);

        // Query 4: mix of constant subexpressions and column references
        const string mixedQuery = @"
            SELECT
                Value + (100 + 200),
                ExpensiveCompute(Value) + (10 * 5),
                Name
            FROM #test.entities()
            WHERE Value > (5 + 5) AND ExpensiveCompute(Value) > (50 + 50)";

        _mixedColumnConstantFoldingEnabled = InstanceCreator.CompileForExecution(
            mixedQuery, Guid.NewGuid().ToString(),
            schemaProvider, _loggerResolver, FoldingEnabled);

        _mixedColumnConstantFoldingDisabled = InstanceCreator.CompileForExecution(
            mixedQuery, Guid.NewGuid().ToString(),
            schemaProvider, _loggerResolver, FoldingDisabled);
    }

    [Benchmark(Baseline = true)]
    public void Arithmetic_FoldingEnabled()
    {
        _arithmeticFoldingEnabled.Run();
    }

    [Benchmark]
    public void Arithmetic_FoldingDisabled()
    {
        _arithmeticFoldingDisabled.Run();
    }

    [Benchmark]
    public void HeavyConstants_FoldingEnabled()
    {
        _heavyConstantsFoldingEnabled.Run();
    }

    [Benchmark]
    public void HeavyConstants_FoldingDisabled()
    {
        _heavyConstantsFoldingDisabled.Run();
    }

    [Benchmark]
    public void StringConcat_FoldingEnabled()
    {
        _stringConcatFoldingEnabled.Run();
    }

    [Benchmark]
    public void StringConcat_FoldingDisabled()
    {
        _stringConcatFoldingDisabled.Run();
    }

    [Benchmark]
    public void MixedColumnConstant_FoldingEnabled()
    {
        _mixedColumnConstantFoldingEnabled.Run();
    }

    [Benchmark]
    public void MixedColumnConstant_FoldingDisabled()
    {
        _mixedColumnConstantFoldingDisabled.Run();
    }

    private static List<OptBenchEntity> CreateTestData(int count)
    {
        const int randomSeed = 42;
        const int maxRandomValue = 1000;
        var random = new Random(randomSeed);
        var categories = new[] { "A", "B", "C", "D", "E" };

        return Enumerable.Range(0, count)
            .Select(i => new OptBenchEntity
            {
                Id = i,
                Name = $"Entity_{i}",
                Value = random.Next(1, maxRandomValue),
                Category = categories[i % categories.Length]
            })
            .ToList();
    }
}
