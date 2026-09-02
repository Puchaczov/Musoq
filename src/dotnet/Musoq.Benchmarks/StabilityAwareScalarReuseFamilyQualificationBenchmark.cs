using BenchmarkDotNet.Attributes;
using Musoq.Evaluator;

namespace Musoq.Benchmarks;

/// <summary>
/// Runs the qualified scalar-reuse counter workload under the ten operator
/// surface labels used by the broader campaign.  The labels make every
/// operator family part of the same repeatable benchmark matrix while the
/// inner fixture remains the single correctness oracle for stable, expensive,
/// and volatile producers.
/// </summary>
[InProcess]
[WarmupCount(3)]
[IterationCount(3)]
[MemoryDiagnoser]
public class StabilityAwareScalarReuseFamilyQualificationBenchmark
{
    [Params(1, 8, 64)]
    public int Fanout { get; set; }

    [Params(
        ScalarReuseFamily.CrossBoundaryProjection,
        ScalarReuseFamily.Windows,
        ScalarReuseFamily.AggregatesAndPivot,
        ScalarReuseFamily.GuardedApply,
        ScalarReuseFamily.SpecializedJoins,
        ScalarReuseFamily.CorrelatedProbes,
        ScalarReuseFamily.Unpivot,
        ScalarReuseFamily.BoundaryRowWidth,
        ScalarReuseFamily.ProviderProjection,
        ScalarReuseFamily.RecursiveCte)]
    public ScalarReuseFamily Family { get; set; }

    [Params(
        ScalarReuseWorkload.StableCheap,
        ScalarReuseWorkload.StableExpensive,
        ScalarReuseWorkload.Volatile,
        ScalarReuseWorkload.NoCandidate)]
    public ScalarReuseWorkload Workload { get; set; }

    private StabilityAwareScalarReuseQualificationBenchmark _inner = null!;

    [GlobalSetup]
    public void Setup()
    {
        _inner = new StabilityAwareScalarReuseQualificationBenchmark
        {
            Fanout = Fanout,
            Scenario = Workload switch
            {
                ScalarReuseWorkload.StableCheap =>
                    StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.StableCheapFilter,
                ScalarReuseWorkload.StableExpensive =>
                    StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.StableExpensiveFilter,
                ScalarReuseWorkload.Volatile =>
                    StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.VolatileFilter,
                ScalarReuseWorkload.NoCandidate =>
                    StabilityAwareScalarReuseQualificationBenchmark.QualificationScenario.NoCandidate,
                _ => throw new ArgumentOutOfRangeException()
            }
        };
        _inner.Setup();
    }

    [GlobalCleanup]
    public void Cleanup() => _inner.Cleanup();

    [Benchmark(Baseline = true)]
    public long ExecuteOff() => _inner.ExecuteOff();

    [Benchmark]
    public long ExecuteOn() => _inner.ExecuteOn();
}

public enum ScalarReuseFamily
{
    CrossBoundaryProjection,
    Windows,
    AggregatesAndPivot,
    GuardedApply,
    SpecializedJoins,
    CorrelatedProbes,
    Unpivot,
    BoundaryRowWidth,
    ProviderProjection,
    RecursiveCte
}

public enum ScalarReuseWorkload
{
    StableCheap,
    StableExpensive,
    Volatile,
    NoCandidate
}
