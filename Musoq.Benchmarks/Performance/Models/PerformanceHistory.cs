using Newtonsoft.Json;

namespace Musoq.Benchmarks.Performance.Models;

public class PerformanceHistory
{
    [JsonProperty("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    [JsonProperty("benchmarks")]
    public Dictionary<string, List<BenchmarkResult>> Benchmarks { get; set; } = new();

    public void AddResult(BenchmarkResult result)
    {
        if (!Benchmarks.ContainsKey(result.Method))
        {
            Benchmarks[result.Method] = new List<BenchmarkResult>();
        }

        Benchmarks[result.Method].Add(result);
        
        // Keep only last 100 results per benchmark
        if (Benchmarks[result.Method].Count > 100)
        {
            Benchmarks[result.Method] = Benchmarks[result.Method]
                .OrderByDescending(r => r.Timestamp)
                .Take(100)
                .OrderBy(r => r.Timestamp)
                .ToList();
        }

        LastUpdated = DateTime.UtcNow;
    }

    public List<BenchmarkResult> GetHistoryForMethod(string method)
    {
        return Benchmarks.TryGetValue(method, out var history) 
            ? history.OrderBy(r => r.Timestamp).ToList() 
            : new List<BenchmarkResult>();
    }

    public BenchmarkResult? GetLatestResult(string method)
    {
        var history = GetHistoryForMethod(method);
        return history.LastOrDefault();
    }

    public BenchmarkResult? GetPreviousResult(string method)
    {
        var history = GetHistoryForMethod(method);
        return history.Count >= 2 ? history[^2] : null;
    }
}