namespace Musoq.Benchmarks.Performance;

internal sealed record BenchmarkComparison(
    string Method,
    BenchmarkMetric Baseline,
    BenchmarkMetric Current,
    double TimeRatio,
    double AllocationRatio,
    bool IsRegression);
