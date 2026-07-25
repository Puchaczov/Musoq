using Musoq.Evaluator;

namespace Musoq.Benchmarks;

internal static class RecursiveCteBenchmarkOptions
{
    public static CompilationOptions Create(
        RecursiveCteBenchmarkFixture fixture,
        ParallelizationMode parallelizationMode)
    {
        var maximumRows = Math.Max(32, fixture.Edges.Length + 2);
        var maximumIterations = Math.Max(32, maximumRows);
        return BenchmarkCompilationOptions.Materialized(
                new CompilationOptions(
                    parallelizationMode: parallelizationMode,
                    useHashJoin: true,
                    useSortMergeJoin: false))
            .WithRecursiveCteLimits(new RecursiveCteExecutionLimits(maximumIterations, maximumRows));
    }
}
