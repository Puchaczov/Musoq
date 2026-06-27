using Musoq.Evaluator;

namespace Musoq.Benchmarks;

internal static class BenchmarkCompilationOptions
{
    public static CompilationOptions Materialized(CompilationOptions? options = null)
    {
        return (options ?? new CompilationOptions()).WithTableResultMaterialization();
    }
}
