using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Musoq.Benchmarks;

/// <summary>
/// Establishes a target-neutral recomputation baseline for the scalar reuse
/// surfaces that are exercised by the optimizer qualification suite.
/// </summary>
[MemoryDiagnoser]
[WarmupCount(2)]
[IterationCount(3)]
public sealed class StableScalarReuseBaselineBenchmark
{
    private const int OuterRowCount = 2;
    private int[] _outer = null!;
    private int[] _middle = null!;
    private int[] _leaf = null!;

    [Params(1, 8, 64)]
    public int Fanout { get; set; }

    [Params(
        ScalarReuseBaselineSurface.CrossBoundary,
        ScalarReuseBaselineSurface.Window,
        ScalarReuseBaselineSurface.Aggregate,
        ScalarReuseBaselineSurface.Pivot,
        ScalarReuseBaselineSurface.GuardedApply,
        ScalarReuseBaselineSurface.SpecializedJoin,
        ScalarReuseBaselineSurface.CorrelatedProbe,
        ScalarReuseBaselineSurface.Unpivot,
        ScalarReuseBaselineSurface.BoundaryWidth,
        ScalarReuseBaselineSurface.ProviderProjection,
        ScalarReuseBaselineSurface.RecursiveInvariant)]
    public ScalarReuseBaselineSurface Surface { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var size = Math.Max(1, Fanout);
        _outer = Enumerable.Range(1, OuterRowCount).ToArray();
        _middle = Enumerable.Range(1, size).ToArray();
        _leaf = Enumerable.Range(1, size).ToArray();
    }

    [Benchmark(Baseline = true)]
    public int RecomputeAtUseSite() => Execute(hoist: false);

    [Benchmark]
    public int HandHoistedReference() => Execute(hoist: true);

    private int Execute(bool hoist)
    {
        var checksum = 0;
        foreach (var outer in _outer)
        {
            var outerValue = hoist ? Expensive(outer) : 0;
            foreach (var middle in _middle)
            {
                var middleValue = hoist ? Expensive(outer + middle) : 0;
                foreach (var leaf in _leaf)
                {
                    var outerTerm = hoist ? outerValue : Expensive(outer);
                    var middleTerm = hoist ? middleValue : Expensive(outer + middle);
                    var leafTerm = Expensive(outer + middle + leaf);
                    checksum = unchecked(checksum + Compose(Surface, outerTerm, middleTerm, leafTerm));
                }
            }
        }

        return checksum;
    }

    private static int Compose(
        ScalarReuseBaselineSurface surface,
        int outer,
        int middle,
        int leaf)
    {
        return surface switch
        {
            ScalarReuseBaselineSurface.Aggregate => unchecked(outer + middle),
            ScalarReuseBaselineSurface.Pivot => (outer ^ middle) == 0 ? leaf : outer,
            ScalarReuseBaselineSurface.GuardedApply => middle > 0 ? outer + leaf : 0,
            ScalarReuseBaselineSurface.SpecializedJoin => outer == middle ? leaf : middle,
            ScalarReuseBaselineSurface.CorrelatedProbe => unchecked(outer * 31 + middle),
            ScalarReuseBaselineSurface.Unpivot => unchecked(outer + leaf),
            ScalarReuseBaselineSurface.BoundaryWidth => unchecked(outer + middle + leaf),
            ScalarReuseBaselineSurface.ProviderProjection => unchecked(outer ^ middle ^ leaf),
            ScalarReuseBaselineSurface.RecursiveInvariant => unchecked(outer + leaf),
            _ => unchecked(outer + middle + leaf)
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Expensive(int value)
    {
        var result = value + 17;
        for (var index = 0; index < 64; index++)
            result = unchecked(result * 31 + index);

        return result;
    }
}

public enum ScalarReuseBaselineSurface
{
    CrossBoundary,
    Window,
    Aggregate,
    Pivot,
    GuardedApply,
    SpecializedJoin,
    CorrelatedProbe,
    Unpivot,
    BoundaryWidth,
    ProviderProjection,
    RecursiveInvariant
}
