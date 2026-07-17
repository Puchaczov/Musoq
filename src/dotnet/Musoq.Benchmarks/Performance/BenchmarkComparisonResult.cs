using System.Collections.Generic;
using System.Linq;

namespace Musoq.Benchmarks.Performance;

internal sealed record BenchmarkComparisonResult(IReadOnlyList<BenchmarkComparison> Comparisons)
{
    public bool IsSuccess => Comparisons.All(static comparison => !comparison.IsRegression);
}
