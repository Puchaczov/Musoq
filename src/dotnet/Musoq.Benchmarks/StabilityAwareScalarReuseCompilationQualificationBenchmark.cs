using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Benchmarks;

/// <summary>
/// Keeps compilation and artifact-cache costs in the same qualification
/// campaign as execution.  The benchmark intentionally uses unique assembly
/// names so it measures compilation work rather than a runtime memoization
/// branch.
/// </summary>
[InProcess]
[WarmupCount(3)]
[IterationCount(3)]
[MemoryDiagnoser]
public sealed class StabilityAwareScalarReuseCompilationQualificationBenchmark
{
    private readonly ILoggerResolver _loggerResolver = new BenchmarkLoggerResolver();
    private readonly ISchemaProvider _provider = CreateProvider();
    private int _ordinal;

    [Benchmark(Baseline = true)]
    public int CompileTinyNoOp()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select i.Id from #reuse.items() i",
            $"ScalarReuseTiny_{Interlocked.Increment(ref _ordinal)}",
            _provider,
            _loggerResolver,
            new CompilationOptions().WithStabilityAwareScalarReuse(false));
        return inspection.GeneratedCSharpCode.Length;
    }

    [Benchmark]
    public int CompileFeatureHeavy()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select i.ExpensiveValue from #reuse.items() i where i.ExpensiveValue > 0 order by i.Id",
            $"ScalarReuseFeature_{Interlocked.Increment(ref _ordinal)}",
            _provider,
            _loggerResolver,
            new CompilationOptions().WithStabilityAwareScalarReuse(true));
        return inspection.GeneratedCSharpCode.Length;
    }

    private static ISchemaProvider CreateProvider() =>
        new StabilityAwareScalarReuseQualificationBenchmark.ReuseSchemaProvider(
            Enumerable.Range(1, 8)
                .Select(id => new StabilityAwareScalarReuseQualificationBenchmark.ReuseRow { Id = id })
                .ToArray());
}
