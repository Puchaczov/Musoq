using System.Text.Json;
using System.Text.Json.Serialization;

namespace Musoq.Benchmarks.Performance;

internal static class BenchmarkReportReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyDictionary<string, BenchmarkMetric> Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Benchmark report path cannot be empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("Benchmark report was not found.", path);

        using var stream = File.OpenRead(path);
        var report = JsonSerializer.Deserialize<BenchmarkReport>(stream, SerializerOptions)
                     ?? throw new InvalidDataException($"Benchmark report '{path}' is empty.");

        if (report.Benchmarks is not { Count: > 0 })
            throw new InvalidDataException($"Benchmark report '{path}' contains no benchmarks.");

        var metrics = new Dictionary<string, BenchmarkMetric>(StringComparer.Ordinal);
        foreach (var benchmark in report.Benchmarks)
        {
            var benchmarkName = string.IsNullOrWhiteSpace(benchmark.FullName)
                ? benchmark.Method
                : benchmark.FullName;
            if (string.IsNullOrWhiteSpace(benchmarkName))
                throw new InvalidDataException($"Benchmark report '{path}' contains a benchmark without a method name.");

            if (benchmark.Statistics is null ||
                !double.IsFinite(benchmark.Statistics.Mean) ||
                benchmark.Statistics.Mean <= 0)
            {
                throw new InvalidDataException(
                    $"Benchmark '{benchmarkName}' in '{path}' has no valid timing statistics. " +
                    "The report is partial and cannot be used for a performance gate.");
            }

            var allocatedBytes = benchmark.Memory?.BytesAllocatedPerOperation;
            if (!allocatedBytes.HasValue ||
                !double.IsFinite(allocatedBytes.Value) ||
                allocatedBytes.Value < 0)
            {
                throw new InvalidDataException(
                    $"Benchmark '{benchmarkName}' in '{path}' has no valid allocation statistics. " +
                    "Run it with MemoryDiagnoser enabled.");
            }

            if (!metrics.TryAdd(
                    benchmarkName,
                    new BenchmarkMetric(benchmark.Statistics.Mean, allocatedBytes.Value)))
            {
                throw new InvalidDataException(
                    $"Benchmark report '{path}' contains duplicate method '{benchmarkName}'.");
            }
        }

        return metrics;
    }

    private sealed class BenchmarkReport
    {
        [JsonPropertyName("Benchmarks")]
        public List<BenchmarkRecord>? Benchmarks { get; init; }
    }

    private sealed class BenchmarkRecord
    {
        [JsonPropertyName("Method")]
        public string? Method { get; init; }

        [JsonPropertyName("FullName")]
        public string? FullName { get; init; }

        [JsonPropertyName("Statistics")]
        public BenchmarkStatistics? Statistics { get; init; }

        [JsonPropertyName("Memory")]
        public BenchmarkMemory? Memory { get; init; }
    }

    private sealed class BenchmarkStatistics
    {
        [JsonPropertyName("Mean")]
        public double Mean { get; init; }
    }

    private sealed class BenchmarkMemory
    {
        [JsonPropertyName("BytesAllocatedPerOperation")]
        public double? BytesAllocatedPerOperation { get; init; }
    }
}
