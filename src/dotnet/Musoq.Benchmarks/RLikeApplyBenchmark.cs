using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

/// <summary>Measures inner-bound and outer-stable RLIKE patterns after correlated APPLY.</summary>
[MemoryDiagnoser]
[ShortRunJob]
public class RLikeApplyBenchmark : IDisposable
{
    private const int DefaultOuterRowCount = 1_024;
    private readonly DynamicLikeApplyRecorder _recorder = new();
    private CompiledQuery _innerPattern = null!;
    private CompiledQuery _outerPattern = null!;

    [Params(1, 8, 64)]
    public int FanOut { get; set; }

    internal int OuterRowCount { get; set; } = DefaultOuterRowCount;

    public string InnerPatternResultHash { get; private set; } = string.Empty;

    public string OuterPatternResultHash { get; private set; } = string.Empty;

    public int ExpectedResultCount => OuterRowCount * FanOut;

    public int SourceInvocations => _recorder.SourceInvocations;

    public int ChildrenReads => _recorder.ChildrenReads;

    public int PatternReads => _recorder.PatternReads;

    [GlobalSetup]
    public void Setup()
    {
        var provider = new DynamicLikeApplySchemaProvider(CreateRows(OuterRowCount, FanOut, _recorder), _recorder);
        var options = BenchmarkCompilationOptions.Materialized(new CompilationOptions(ParallelizationMode.None));
        _innerPattern = Compile(InnerPatternQuery, provider, options);
        _outerPattern = Compile(OuterPatternQuery, provider, options);

        using (var result = _innerPattern.Run())
        {
            ValidateResult(result);
            InnerPatternResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
        }

        using (var result = _outerPattern.Run())
        {
            ValidateResult(result);
            OuterPatternResultHash = DynamicLikeBenchmarkData.ComputeResultHash(result);
        }

        _recorder.Reset();
    }

    public void ResetCounters() => _recorder.Reset();

    [Benchmark(Baseline = true)]
    public Table RLikeApply_InnerPattern() => _innerPattern.Run();

    [Benchmark]
    public Table RLikeApply_OuterPattern() => _outerPattern.Run();

    internal static (QueryInspectionResult Inner, QueryInspectionResult Outer) InspectQueries()
    {
        var recorder = new DynamicLikeApplyRecorder();
        var provider = new DynamicLikeApplySchemaProvider(CreateRows(4, 2, recorder), recorder);
        var options = BenchmarkCompilationOptions.Materialized(new CompilationOptions(ParallelizationMode.None));
        var logger = new BenchmarkLoggerResolver();
        return (
            InstanceCreator.CompileForInspection(InnerPatternQuery, $"RLikeApplyInner_{Guid.NewGuid():N}", provider, logger, options),
            InstanceCreator.CompileForInspection(OuterPatternQuery, $"RLikeApplyOuter_{Guid.NewGuid():N}", provider, logger, options));
    }

    internal static DynamicLikeApplyOuter[] CreateRows(
        int outerRowCount,
        int fanOut,
        DynamicLikeApplyRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        return Enumerable.Range(0, outerRowCount)
            .Select(outerIndex =>
            {
                var identity = outerIndex.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
                var input = $"outer-{identity}-value";
                var children = Enumerable.Range(0, fanOut)
                    .Select(childIndex => new DynamicLikeApplyChild
                    {
                        Id = childIndex,
                        Pattern = childIndex % 2 == 0
                            ? string.Concat(@"\A", input, @"\z")
                            : string.Concat(@"\Aouter-", identity, @"-.+\z"),
                        Value = $"child-{identity}-{childIndex:D2}"
                    })
                    .ToArray();
                return new DynamicLikeApplyOuter(
                    recorder,
                    outerIndex,
                    input,
                    string.Concat(@"\Achild-", identity, @"-[0-9]{2}\z"),
                    children);
            })
            .ToArray();
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _innerPattern?.Dispose();
        _outerPattern?.Dispose();
    }

    internal const string InnerPatternQuery =
        "select m1.Id, m2.Id as ChildId from #dynamicLike.items() m1 " +
        "cross apply m1.Children m2 where m1.Input rlike m2.Pattern";

    internal const string OuterPatternQuery =
        "select m1.Id, m2.Id as ChildId from #dynamicLike.items() m1 " +
        "cross apply m1.Children m2 where m2.Value rlike m1.Pattern";

    private static CompiledQuery Compile(
        string query,
        DynamicLikeApplySchemaProvider provider,
        CompilationOptions options) => InstanceCreator.CompileForExecution(
        query,
        $"RLikeApply_{Guid.NewGuid():N}",
        provider,
        new BenchmarkLoggerResolver(),
        options);

    private void ValidateResult(Table result)
    {
        if (result.Count != ExpectedResultCount)
            throw new InvalidOperationException($"Expected {ExpectedResultCount} rows, received {result.Count}.");
    }
}
