namespace Musoq.Benchmarks.Performance;

internal sealed record BenchmarkMetric(double MeanNanoseconds, double AllocatedBytes);
